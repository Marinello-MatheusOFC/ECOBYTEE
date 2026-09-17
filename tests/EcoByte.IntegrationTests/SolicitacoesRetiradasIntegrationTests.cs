using System.Net;
using EcoByte.IntegrationTests;
using EcoByte.Web.Enums;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

public class SolicitacoesRetiradasIntegrationTests
{
    static SolicitacoesRetiradasIntegrationTests()
    {
        AmbienteEmulador.ConfigurarAmbienteSeguro();
    }

    private static ServiceProvider Servicos => ServicosIntegracao.Provider();

    private static string EmailUnico(string prefixo)
        => $"{prefixo}-{Guid.NewGuid():N}@exemplo.com";

    private static async Task<(Produto Produto, string UidConsumidor, string UidParceiro, string IdEstabelecimento)>
        CriarProdutoComParticipantesAsync(string nomeProduto, int quantidade, StatusProduto status)
    {
        var (uidConsumidor, _) = await CenarioIntegracao.RegistrarConsumidorAsync(Servicos, EmailUnico("cons"));
        var (uidParceiro, idEstabelecimento) =
            await CenarioIntegracao.ParceiroComEstabelecimentoAsync(Servicos, EmailUnico("parc"));

        var repositorio = Servicos.GetRequiredService<IProdutoRepository>();
        var produto = new Produto
        {
            Nome = nomeProduto,
            Descricao = "Produto de teste",
            PrecoOriginalCentavos = 1000,
            PrecoPromocionalCentavos = 400,
            QuantidadeDisponivel = quantidade,
            DataLimite = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(3)),
            Categoria = CategoriaProduto.Padaria.ToFirestoreString(),
            Status = status.ToFirestoreString(),
            EhVegano = true,
            EhSemGluten = false,
            EhSemLactose = false,
            EstabelecimentoId = idEstabelecimento,
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        await repositorio.CriarAsync(produto);

        return (produto, uidConsumidor, uidParceiro, idEstabelecimento);
    }

    private static async Task<Solicitacao> RecarregarSolicitacaoAsync(string id)
    {
        var repositorio = Servicos.GetRequiredService<ISolicitacaoRepository>();
        return (await repositorio.ObterPorIdAsync(id))!;
    }

    private static async Task<Produto> RecarregarProdutoAsync(string id)
    {
        var repositorio = Servicos.GetRequiredService<IProdutoRepository>();
        return (await repositorio.ObterPorIdAsync(id))!;
    }

    private static async Task<int> ContarLogsAsync(string entidade, string entidadeId)
    {
        var db = Servicos.GetRequiredService<FirestoreDb>();
        var snapshot = await db.Collection("logs")
            .WhereEqualTo("entidade", entidade)
            .WhereEqualTo("entidadeId", entidadeId)
            .GetSnapshotAsync();
        return snapshot.Count;
    }

    [SkippableFact]
    public async Task CriarSolicitacao_FluxoCompleto_ReservaEstoque_ERegistraLog()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, idEstabelecimento) =
            await CriarProdutoComParticipantesAsync("pao integral", 10, StatusProduto.Disponivel);
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        var id = await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 4);

        var persistida = await RecarregarSolicitacaoAsync(id);
        Assert.Equal(produto.Id, persistida.ProdutoId);
        Assert.Equal(uidConsumidor, persistida.ConsumidorId);
        Assert.Equal(idEstabelecimento, persistida.EstabelecimentoId);
        Assert.Equal(4, persistida.Quantidade);
        Assert.Equal(StatusSolicitacao.Pendente.ToString(), persistida.Status);
        Assert.NotNull(persistida.ReservadoAte);

        var produtoAtualizado = await RecarregarProdutoAsync(produto.Id!);
        Assert.Equal(6, produtoAtualizado.QuantidadeDisponivel);

        Assert.True(await ContarLogsAsync("Solicitacao", id) >= 1);
    }

    [SkippableFact]
    public async Task CriarSolicitacao_EstoqueZerado_MarcaProdutoComoEsgotado()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, _) =
            await CriarProdutoComParticipantesAsync("queijo minas", 2, StatusProduto.Disponivel);
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();
        var repositorio = Servicos.GetRequiredService<IProdutoRepository>();

        await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 2);

        var produtoAtualizado = await RecarregarProdutoAsync(produto.Id!);
        Assert.Equal(0, produtoAtualizado.QuantidadeDisponivel);
        Assert.Equal(StatusProduto.Esgotado.ToFirestoreString(), produtoAtualizado.Status);
    }

    [SkippableFact]
    public async Task CriarSolicitacao_EstoqueInsuficiente_NaoCria_NemAlteraEstoque()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, _) =
            await CriarProdutoComParticipantesAsync("coxinha", 2, StatusProduto.Disponivel);
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 5));

        var produtoAtualizado = await RecarregarProdutoAsync(produto.Id!);
        Assert.Equal(2, produtoAtualizado.QuantidadeDisponivel);
    }

    [SkippableFact]
    public async Task CriarSolicitacao_ProdutoIndisponivel_RejeitadoSemMudarEstoque()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, _) =
            await CriarProdutoComParticipantesAsync("torta", 5, StatusProduto.Rascunho);
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 1));
    }

    [SkippableFact]
    public async Task Solicitacoes_DoConsumidor_SaoEscopadas()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, _) =
            await CriarProdutoComParticipantesAsync("pao de sal", 10, StatusProduto.Disponivel);
        var (outroConsumidor, _) = await CenarioIntegracao.RegistrarConsumidorAsync(Servicos, EmailUnico("cons"));
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 2);

        var doConsumidor = await solicitacoes.ObterPorConsumidorAsync(uidConsumidor, 1, 50);
        var doOutro = await solicitacoes.ObterPorConsumidorAsync(outroConsumidor, 1, 50);

        Assert.Equal(1, await solicitacoes.ContarPorConsumidorAsync(uidConsumidor));
        Assert.Contains(doConsumidor, s => s.Quantidade == 2);
        Assert.Empty(doOutro);
    }

    [SkippableFact]
    public async Task Solicitacoes_DoEstabelecimento_SaoEscopadas()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, _) =
            await CriarProdutoComParticipantesAsync("pizza", 10, StatusProduto.Disponivel);
        var (_, outroEstabelecimento) =
            await CenarioIntegracao.ParceiroComEstabelecimentoAsync(Servicos, EmailUnico("parc"));
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 3);

        Assert.Equal(1, await solicitacoes.ContarPorEstabelecimentoAsync(produto.EstabelecimentoId));
        Assert.Equal(0, await solicitacoes.ContarPorEstabelecimentoAsync(outroEstabelecimento));
    }

    [SkippableFact]
    public async Task Cancelar_ProprioConsumidor_MarcaCancelada()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, _) =
            await CriarProdutoComParticipantesAsync("suspiro", 10, StatusProduto.Disponivel);
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        var id = await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 2);
        await solicitacoes.CancelarAsync(id, uidConsumidor);

        var persistida = await RecarregarSolicitacaoAsync(id);
        Assert.Equal(StatusSolicitacao.Cancelada.ToString(), persistida.Status);
        Assert.NotNull(persistida.CanceladoEm);
        Assert.True(await ContarLogsAsync("Solicitacao", id) >= 2);
    }

    [SkippableFact]
    public async Task Cancelar_SolicitacaoDeOutroConsumidor_Rejeitado()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, _) =
            await CriarProdutoComParticipantesAsync("bolo", 10, StatusProduto.Disponivel);
        var (naoDono, _) = await CenarioIntegracao.RegistrarConsumidorAsync(Servicos, EmailUnico("cons"));
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        var id = await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 2);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => solicitacoes.CancelarAsync(id, naoDono));
    }

    [SkippableFact]
    public async Task ConfirmarRetirada_PeloDonoDoEstabelecimento_MarcaConcluida()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, uidParceiro, _) =
            await CriarProdutoComParticipantesAsync("marmita", 10, StatusProduto.Disponivel);
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        var id = await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 2);
        await solicitacoes.ConfirmarRetiradaAsync(id, uidParceiro);

        var persistida = await RecarregarSolicitacaoAsync(id);
        Assert.Equal(StatusSolicitacao.Concluida.ToString(), persistida.Status);
        Assert.NotNull(persistida.ConcluidoEm);
        Assert.True(await ContarLogsAsync("Solicitacao", id) >= 2);
    }

    [SkippableFact]
    public async Task ConfirmarRetirada_PorNaoDonoDoEstabelecimento_Rejeitado()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, _, _) =
            await CriarProdutoComParticipantesAsync("cuscuz", 10, StatusProduto.Disponivel);
        var (intruso, _) = await CenarioIntegracao.ParceiroComEstabelecimentoAsync(Servicos, EmailUnico("parc"));
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        var id = await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 2);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => solicitacoes.ConfirmarRetiradaAsync(id, intruso));
    }

    [SkippableFact]
    public async Task ConfirmarRetirada_SolicitacaoCancelada_Rejeitado()
    {
        EmuladorGuarda.Validar();

        var (produto, uidConsumidor, uidParceiro, _) =
            await CriarProdutoComParticipantesAsync("esfirra", 10, StatusProduto.Disponivel);
        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        var id = await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 2);
        await solicitacoes.CancelarAsync(id, uidConsumidor);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => solicitacoes.ConfirmarRetiradaAsync(id, uidParceiro));
    }

    private static async Task<(string Uid, string Token)> RegistrarUsuarioNoEmuladorAsync(string nome, string email, string perfil)
    {
        var contas = Servicos.GetRequiredService<IAuthGateway>();
        var validador = Servicos.GetRequiredService<IFirebaseTokenValidator>();

        var credenciais = await contas.RegistrarComSenhaAsync(email, "Senha123!");
        var validacao = await validador.ValidarAsync(credenciais.IdToken!);
        Assert.True(validacao.Valido, validacao.Erro);

        var uid = validacao.Uid!;
        var criouUsuario = await EmuladorRest.CriarAsync(
            credenciais.IdToken!, "usuarios", uid,
            EmuladorRest.CamposUsuario(uid, nome, email, perfil));
        Assert.Equal(HttpStatusCode.OK, criouUsuario.StatusCode);

        return (uid, credenciais.IdToken!);
    }

    [SkippableFact]
    public async Task Regras_Consumidor_CriaSolicitacao_DonoELe_EstrangeiroNao()
    {
        EmuladorGuarda.Validar();

        var (consumidorUid, tokenConsumidor) =
            await RegistrarUsuarioNoEmuladorAsync("Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (parceiroUid, tokenParceiro) =
            await RegistrarUsuarioNoEmuladorAsync("Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var (estranhoUid, tokenEstranho) =
            await RegistrarUsuarioNoEmuladorAsync("Consumidor B", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var idEstabelecimento = "estab-sol-" + Guid.NewGuid().ToString("N");
        var idProduto = "prod-sol-" + Guid.NewGuid().ToString("N");
        var idSolicitacao = "sol-rules-" + Guid.NewGuid().ToString("N");

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenParceiro, "estabelecimentos", idEstabelecimento,
                EmuladorRest.CamposEstabelecimento(parceiroUid, "Padaria da Ana"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenParceiro, "produtos", idProduto,
                EmuladorRest.CamposProduto(idEstabelecimento, 10))).StatusCode);

        var criou = await EmuladorRest.CriarAsync(
            tokenConsumidor, "solicitacoes", idSolicitacao,
            EmuladorRest.CamposSolicitacao(idProduto, consumidorUid, idEstabelecimento, 2));
        Assert.Equal(HttpStatusCode.OK, criou.StatusCode);

        var donoLe = await EmuladorRest.ObterAsync(tokenConsumidor, "solicitacoes", idSolicitacao);
        Assert.Equal(HttpStatusCode.OK, donoLe.StatusCode);

        var donoEstabelecimentoLe = await EmuladorRest.ObterAsync(tokenParceiro, "solicitacoes", idSolicitacao);
        Assert.Equal(HttpStatusCode.OK, donoEstabelecimentoLe.StatusCode);

        var estranhoLe = await EmuladorRest.ObterAsync(tokenEstranho, "solicitacoes", idSolicitacao);
        Assert.Equal(HttpStatusCode.Forbidden, estranhoLe.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_Parceiro_NaoCriaSolicitacaoComoConsumidor()
    {
        EmuladorGuarda.Validar();

        var (parceiroUid, tokenParceiro) =
            await RegistrarUsuarioNoEmuladorAsync("Dona Bea", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());

        var idSolicitacao = "sol-rules-" + Guid.NewGuid().ToString("N");
        var criou = await EmuladorRest.CriarAsync(
            tokenParceiro, "solicitacoes", idSolicitacao,
            EmuladorRest.CamposSolicitacao("produto-qualquer", parceiroUid, "estab-qualquer", 2));

        Assert.Equal(HttpStatusCode.Forbidden, criou.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_SolicitacaoSemStatusPendente_Rejeitada()
    {
        EmuladorGuarda.Validar();

        var (consumidorUid, tokenConsumidor) =
            await RegistrarUsuarioNoEmuladorAsync("Consumidor C", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var campos = EmuladorRest.CamposSolicitacao("produto-x", consumidorUid, "estab-x", 2);
        campos["status"] = "Confirmada";

        var criou = await EmuladorRest.CriarAsync(
            tokenConsumidor, "solicitacoes", "sol-rules-" + Guid.NewGuid().ToString("N"), campos);

        Assert.Equal(HttpStatusCode.Forbidden, criou.StatusCode);
    }
}