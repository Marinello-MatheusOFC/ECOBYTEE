using System.Net;
using System.Text.Json;
using EcoByte.IntegrationTests;
using EcoByte.Web.Enums;
using EcoByte.Web.Interfaces;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

public class FirestoreRulesIntegrationTests
{
    static FirestoreRulesIntegrationTests()
    {
        AmbienteEmulador.ConfigurarAmbienteSeguro();
    }

    private static ServiceProvider Servicos => ServicosIntegracao.Provider();

    private static string EmailUnico(string prefixo)
        => $"{prefixo}-{Guid.NewGuid():N}@exemplo.com";

    private static FirestoreDb CriarDbAdmin()
        => new FirestoreDbBuilder
        {
            ProjectId = AmbienteEmulador.ProjetoAuthAdmin,
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();

    private static async Task<(string Uid, string Token)> RegistrarNoEmuladorAsync(string nome, string email, string perfil)
    {
        var contas = Servicos.GetRequiredService<IAuthGateway>();
        var validador = Servicos.GetRequiredService<IFirebaseTokenValidator>();

        var credenciais = await contas.RegistrarComSenhaAsync(email, "Senha123!");
        var validacao = await validador.ValidarAsync(credenciais.IdToken!);
        Assert.True(validacao.Valido, validacao.Erro);

        var uid = validacao.Uid!;
        var criou = await EmuladorRest.CriarAsync(
            credenciais.IdToken!, "usuarios", uid,
            EmuladorRest.CamposUsuario(uid, nome, email, perfil));
        Assert.Equal(HttpStatusCode.OK, criou.StatusCode);

        return (uid, credenciais.IdToken!);
    }

    private static async Task<(string Uid, string Token)> RegistrarAdminAsync(string email)
    {
        var contas = Servicos.GetRequiredService<IAuthGateway>();
        var validador = Servicos.GetRequiredService<IFirebaseTokenValidator>();

        var credenciais = await contas.RegistrarComSenhaAsync(email, "Senha123!");
        var validacao = await validador.ValidarAsync(credenciais.IdToken!);
        Assert.True(validacao.Valido, validacao.Erro);

        var uid = validacao.Uid!;

        var dbAdmin = CriarDbAdmin();
        await dbAdmin.Collection("usuarios").Document(uid).SetAsync(new
        {
            uid,
            nome = "Administrador Teste",
            email,
            perfil = PerfilUsuario.Administrador.ToString(),
            ativo = true,
            criadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        });

        return (uid, credenciais.IdToken!);
    }

    private static async Task<string> CriarEstabelecimentoAsync(string token, string parceiroUid, string nomeFantasia)
    {
        var id = "estab-rules-" + Guid.NewGuid().ToString("N");
        var resposta = await EmuladorRest.CriarAsync(
            token, "estabelecimentos", id,
            EmuladorRest.CamposEstabelecimento(parceiroUid, nomeFantasia));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return id;
    }

    private static async Task<string> CriarProdutoAsync(string token, string estabelecimentoId)
    {
        var id = "prod-rules-" + Guid.NewGuid().ToString("N");
        var resposta = await EmuladorRest.CriarAsync(
            token, "produtos", id,
            EmuladorRest.CamposProduto(estabelecimentoId, 10));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return id;
    }

    private static async Task<string> CriarSolicitacaoAsync(string token, string consumidorUid, string estabelecimentoId, string produtoId)
    {
        var id = "sol-rules-" + Guid.NewGuid().ToString("N");
        var resposta = await EmuladorRest.CriarAsync(
            token, "solicitacoes", id,
            EmuladorRest.CamposSolicitacao(produtoId, consumidorUid, estabelecimentoId, 2));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return id;
    }

    private static bool FoiNegado(HttpStatusCode status)
        => status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    private static async Task<string?> LerCampoStringAsync(string token, string colecao, string documentId, string campo)
    {
        var resposta = await EmuladorRest.ObterAsync(token, colecao, documentId);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var json = await resposta.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(json);
        return documento.RootElement
            .GetProperty("fields")
            .GetProperty(campo)
            .GetProperty("stringValue")
            .GetString();
    }

    [SkippableFact]
    public async Task Regras_Usuario_LeProprioDoc_AtualizaNome_MasNaoAtivo()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterAsync(token, "usuarios", uid)).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.AtualizarAsync(
                token, "usuarios", uid,
                new Dictionary<string, object> { ["nome"] = "Novo Nome" })).StatusCode);

        var mudouAtivo = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["ativo"] = false });
        Assert.True(FoiNegado(mudouAtivo.StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Usuario_CriacaoComUidAlheio_SemAutenticacao_Rejeitada()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var uidAlheio = "uid-alheio-" + Guid.NewGuid().ToString("N");
        var criouAlheio = await EmuladorRest.CriarAsync(
            token, "usuarios", uidAlheio,
            EmuladorRest.CamposUsuario(uidAlheio, "Outra Pessoa", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString()));
        Assert.True(FoiNegado(criouAlheio.StatusCode));

        var semAutenticacao = await EmuladorRest.CriarAsync(
            string.Empty, "usuarios", uid,
            EmuladorRest.CamposUsuario(uid, "Anonimo", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString()));
        Assert.True(FoiNegado(semAutenticacao.StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Usuario_Delete_SomenteAdministrador()
    {
        EmuladorGuarda.Validar();

        var (uidAlvo, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());

        var (_, tokenAdmin) = await RegistrarAdminAsync(EmailUnico("admin"));

        var parceiroDeletou = await EmuladorRest.ExcluirAsync(tokenParceiro, "usuarios", uidAlvo);
        Assert.True(FoiNegado(parceiroDeletou.StatusCode));

        var adminDeletou = await EmuladorRest.ExcluirAsync(tokenAdmin, "usuarios", uidAlvo);
        Assert.Equal(HttpStatusCode.OK, adminDeletou.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_Conquistas_DoUsuario_EscritaCliente_SempreNegada()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (_, tokenAdmin) = await RegistrarAdminAsync(EmailUnico("admin"));

        var dbAdmin = CriarDbAdmin();
        await dbAdmin.Collection("usuarios").Document(uid)
            .Collection("conquistas").Document("conquista-1").SetAsync(new
            {
                titulo = "Primeira doacao",
                concedidoEm = Timestamp.FromDateTime(DateTime.UtcNow)
            });

        var caminho = $"usuarios/{uid}/conquistas/conquista-1";
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterCaminhoAsync(token, caminho)).StatusCode);

        var campos = new Dictionary<string, object>
        {
            ["titulo"] = "Hack",
            ["concedidoEm"] = DateTime.UtcNow
        };
        var consumidorCriou = await EmuladorRest.CriarSubcolecaoAsync(
            token, $"usuarios/{uid}/conquistas", "conquista-hack", campos);
        Assert.True(FoiNegado(consumidorCriou.StatusCode));

        var adminCriou = await EmuladorRest.CriarSubcolecaoAsync(
            tokenAdmin, $"usuarios/{uid}/conquistas", "conquista-hack", campos);
        Assert.True(FoiNegado(adminCriou.StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Estabelecimento_CriacaoRestritivaParaParceiro()
    {
        EmuladorGuarda.Validar();

        var (consumidorUid, tokenConsumidor) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (parceiroUid, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var (outroUid, _) = await RegistrarNoEmuladorAsync(
            "Dona Bea", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());

        var consumidorCriou = await EmuladorRest.CriarAsync(
            tokenConsumidor, "estabelecimentos", "estab-rules-" + Guid.NewGuid().ToString("N"),
            EmuladorRest.CamposEstabelecimento(consumidorUid, "Casa C"));
        Assert.True(FoiNegado(consumidorCriou.StatusCode));

        var idAlheio = "estab-rules-" + Guid.NewGuid().ToString("N");
        var criouParaOutro = await EmuladorRest.CriarAsync(
            tokenParceiro, "estabelecimentos", idAlheio,
            EmuladorRest.CamposEstabelecimento(outroUid, "Alheio"));
        Assert.True(FoiNegado(criouParaOutro.StatusCode));

        var aprovadoAntes = EmuladorRest.CamposEstabelecimento(parceiroUid, "Aprovado");
        aprovadoAntes["aprovado"] = true;
        var criouAprovado = await EmuladorRest.CriarAsync(
            tokenParceiro, "estabelecimentos", "estab-rules-" + Guid.NewGuid().ToString("N"), aprovadoAntes);
        Assert.True(FoiNegado(criouAprovado.StatusCode));

        await CriarEstabelecimentoAsync(tokenParceiro, parceiroUid, "Padaria da Ana");
    }

    [SkippableFact]
    public async Task Regras_Estabelecimento_DonoNaoMudaResponsavel_AdminDeleta()
    {
        EmuladorGuarda.Validar();

        var (parceiroUid, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var idEstabelecimento = await CriarEstabelecimentoAsync(tokenParceiro, parceiroUid, "Padaria da Ana");

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.AtualizarAsync(
                tokenParceiro, "estabelecimentos", idEstabelecimento,
                new Dictionary<string, object> { ["nomeFantasia"] = "Padaria Novo Nome" })).StatusCode);

        var mudouResponsavel = await EmuladorRest.AtualizarAsync(
            tokenParceiro, "estabelecimentos", idEstabelecimento,
            new Dictionary<string, object> { ["usuarioResponsavelId"] = "outro-uid" });
        Assert.True(FoiNegado(mudouResponsavel.StatusCode));

        var parceiroDeletou = await EmuladorRest.ExcluirAsync(tokenParceiro, "estabelecimentos", idEstabelecimento);
        Assert.True(FoiNegado(parceiroDeletou.StatusCode));

        var (_, tokenAdmin) = await RegistrarAdminAsync(EmailUnico("admin"));
        var adminDeletou = await EmuladorRest.ExcluirAsync(tokenAdmin, "estabelecimentos", idEstabelecimento);
        Assert.Equal(HttpStatusCode.OK, adminDeletou.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_Estabelecimento_LeituraSomenteLogado()
    {
        EmuladorGuarda.Validar();

        var (parceiroUid, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var idEstabelecimento = await CriarEstabelecimentoAsync(tokenParceiro, parceiroUid, "Padaria da Ana");

        var (_, tokenConsumidor) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterAsync(tokenConsumidor, "estabelecimentos", idEstabelecimento)).StatusCode);

        var semAutenticacao = await EmuladorRest.ObterAsync(string.Empty, "estabelecimentos", idEstabelecimento);
        Assert.True(FoiNegado(semAutenticacao.StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Produtos_LeituraPublica_EscritaSomenteDonoOuAdmin()
    {
        EmuladorGuarda.Validar();

        var (parceiroUid, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var idEstabelecimento = await CriarEstabelecimentoAsync(tokenParceiro, parceiroUid, "Padaria da Ana");
        var idProduto = await CriarProdutoAsync(tokenParceiro, idEstabelecimento);

        var (_, tokenConsumidor) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterAsync(string.Empty, "produtos", idProduto)).StatusCode);

        var consumidorCriou = await EmuladorRest.CriarAsync(
            tokenConsumidor, "produtos", "prod-rules-" + Guid.NewGuid().ToString("N"),
            EmuladorRest.CamposProduto(idEstabelecimento, 5));
        Assert.True(FoiNegado(consumidorCriou.StatusCode));

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.AtualizarAsync(
                tokenParceiro, "produtos", idProduto,
                new Dictionary<string, object> { ["quantidadeDisponivel"] = 3 })).StatusCode);

        var mudouEstab = await EmuladorRest.AtualizarAsync(
            tokenParceiro, "produtos", idProduto,
            new Dictionary<string, object> { ["estabelecimentoId"] = "outro-estab" });
        Assert.True(FoiNegado(mudouEstab.StatusCode));

        var donoDeletou = await EmuladorRest.ExcluirAsync(tokenParceiro, "produtos", idProduto);
        Assert.True(FoiNegado(donoDeletou.StatusCode));

        var idProduto2 = await CriarProdutoAsync(tokenParceiro, idEstabelecimento);
        var (_, tokenAdmin) = await RegistrarAdminAsync(EmailUnico("admin"));
        var adminDeletou = await EmuladorRest.ExcluirAsync(tokenAdmin, "produtos", idProduto2);
        Assert.Equal(HttpStatusCode.OK, adminDeletou.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_Solicitacoes_UpdateSomenteDonoEstabelecimentoOuAdmin_DeleteNunca()
    {
        EmuladorGuarda.Validar();

        var (consumidorUid, tokenConsumidor) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (parceiroUid, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());

        var idEstabelecimento = await CriarEstabelecimentoAsync(tokenParceiro, parceiroUid, "Padaria da Ana");
        var idProduto = await CriarProdutoAsync(tokenParceiro, idEstabelecimento);
        var idSolicitacao = await CriarSolicitacaoAsync(tokenConsumidor, consumidorUid, idEstabelecimento, idProduto);

        var consumidorAtualizou = await EmuladorRest.AtualizarAsync(
            tokenConsumidor, "solicitacoes", idSolicitacao,
            new Dictionary<string, object> { ["quantidade"] = 5 });
        Assert.True(FoiNegado(consumidorAtualizou.StatusCode));

        var donoConcluiu = await EmuladorRest.AtualizarAsync(
            tokenParceiro, "solicitacoes", idSolicitacao,
            new Dictionary<string, object> { ["status"] = "Confirmada" });
        Assert.Equal(HttpStatusCode.OK, donoConcluiu.StatusCode);

        var consumidorDeletou = await EmuladorRest.ExcluirAsync(
            tokenConsumidor, "solicitacoes", idSolicitacao);
        Assert.True(FoiNegado(consumidorDeletou.StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Conquistas_Catalogo_LeituraLogada_EscritaSomenteAdmin()
    {
        EmuladorGuarda.Validar();

        var (_, tokenConsumidor) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (_, tokenAdmin) = await RegistrarAdminAsync(EmailUnico("admin"));

        var idConquista = "conquista-cat-" + Guid.NewGuid().ToString("N");
        var campos = new Dictionary<string, object>
        {
            ["titulo"] = "Primeira doacao",
            ["descricao"] = "Doacao realizada",
            ["ativo"] = true
        };

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(
                tokenAdmin, "conquistas", idConquista, campos)).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterAsync(tokenConsumidor, "conquistas", idConquista)).StatusCode);

        var consumidorCriou = await EmuladorRest.CriarAsync(
            tokenConsumidor, "conquistas", "conquista-cat-" + Guid.NewGuid().ToString("N"), campos);
        Assert.True(FoiNegado(consumidorCriou.StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Logs_NinguemEscreve_ForaDoAdminSdk()
    {
        EmuladorGuarda.Validar();

        var (uid, tokenConsumidor) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (_, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var (_, tokenAdmin) = await RegistrarAdminAsync(EmailUnico("admin"));

        var campos = new Dictionary<string, object>
        {
            ["usuarioId"] = uid,
            ["acao"] = "Hack",
            ["entidade"] = "Usuario",
            ["entidadeId"] = uid,
            ["resultado"] = "Sucesso",
            ["criadoEm"] = DateTime.UtcNow
        };

        var consumidorCriou = await EmuladorRest.CriarAsync(
            tokenConsumidor, "logs", "log-hack-" + Guid.NewGuid().ToString("N"), campos);
        Assert.True(FoiNegado(consumidorCriou.StatusCode));

        var parceiroCriou = await EmuladorRest.CriarAsync(
            tokenParceiro, "logs", "log-hack-" + Guid.NewGuid().ToString("N"), campos);
        Assert.True(FoiNegado(parceiroCriou.StatusCode));

        var adminCriou = await EmuladorRest.CriarAsync(
            tokenAdmin, "logs", "log-hack-" + Guid.NewGuid().ToString("N"), campos);
        Assert.True(FoiNegado(adminCriou.StatusCode));
    }

    // ===== Endurecimento: perfil/UID/campos internos =====

    [SkippableFact]
    public async Task Regras_Usuario_AutopromocaoParaParceiro_Negada()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var tentou = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["perfil"] = PerfilUsuario.Parceiro.ToString() });

        Assert.True(FoiNegado(tentou.StatusCode));
        Assert.Equal(PerfilUsuario.Consumidor.ToString(),
            await LerCampoStringAsync(token, "usuarios", uid, "perfil"));
    }

    [SkippableFact]
    public async Task Regras_Usuario_AutopromocaoParaOng_Negada()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var tentou = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["perfil"] = PerfilUsuario.Ong.ToString() });

        Assert.True(FoiNegado(tentou.StatusCode));
        Assert.Equal(PerfilUsuario.Consumidor.ToString(),
            await LerCampoStringAsync(token, "usuarios", uid, "perfil"));
    }

    [SkippableFact]
    public async Task Regras_Usuario_AutopromocaoParaAdministrador_Negada()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var tentou = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["perfil"] = PerfilUsuario.Administrador.ToString() });

        Assert.True(FoiNegado(tentou.StatusCode));
        Assert.Equal(PerfilUsuario.Consumidor.ToString(),
            await LerCampoStringAsync(token, "usuarios", uid, "perfil"));
    }

    [SkippableFact]
    public async Task Regras_Usuario_AtualizaCamposComuns_DoProprioPerfil()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var mudouNome = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["nome"] = "Novo Nome" });
        Assert.Equal(HttpStatusCode.OK, mudouNome.StatusCode);
        Assert.Equal("Novo Nome", await LerCampoStringAsync(token, "usuarios", uid, "nome"));

        var mudouAtualizadoEm = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["atualizadoEm"] = DateTime.UtcNow });
        Assert.Equal(HttpStatusCode.OK, mudouAtualizadoEm.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_Usuario_NaoAlteraPerfil_DeOutroUsuario()
    {
        EmuladorGuarda.Validar();

        var (_, tokenA) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (uidB, tokenB) = await RegistrarNoEmuladorAsync(
            "Consumidor B", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var tentou = await EmuladorRest.AtualizarAsync(
            tokenA, "usuarios", uidB,
            new Dictionary<string, object> { ["perfil"] = PerfilUsuario.Parceiro.ToString() });

        Assert.True(FoiNegado(tentou.StatusCode));
        Assert.Equal(PerfilUsuario.Consumidor.ToString(),
            await LerCampoStringAsync(tokenB, "usuarios", uidB, "perfil"));
    }

    [SkippableFact]
    public async Task Regras_Usuario_NaoAlteraUid_NemCamposInternos()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var mudouUid = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["uid"] = "outro-uid" });
        Assert.True(FoiNegado(mudouUid.StatusCode));

        var mudouAtivo = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["ativo"] = false });
        Assert.True(FoiNegado(mudouAtivo.StatusCode));

        var mudouCriadoEm = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["criadoEm"] = DateTime.UtcNow });
        Assert.True(FoiNegado(mudouCriadoEm.StatusCode));

        Assert.Equal(uid, await LerCampoStringAsync(token, "usuarios", uid, "uid"));
    }

    [SkippableFact]
    public async Task Regras_Administrador_AlteraPerfilDeTerceiro()
    {
        EmuladorGuarda.Validar();

        var (uidAlvo, _) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (_, tokenAdmin) = await RegistrarAdminAsync(EmailUnico("admin"));

        var promoveu = await EmuladorRest.AtualizarAsync(
            tokenAdmin, "usuarios", uidAlvo,
            new Dictionary<string, object> { ["perfil"] = PerfilUsuario.Parceiro.ToString() });

        Assert.Equal(HttpStatusCode.OK, promoveu.StatusCode);
        Assert.Equal(PerfilUsuario.Parceiro.ToString(),
            await LerCampoStringAsync(tokenAdmin, "usuarios", uidAlvo, "perfil"));
    }
}