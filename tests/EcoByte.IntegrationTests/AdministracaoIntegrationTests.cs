using System.Net;
using System.Text.Json;
using EcoByte.IntegrationTests;
using EcoByte.Web.Enums;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.ViewModels;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

public class AdministracaoIntegrationTests
{
    static AdministracaoIntegrationTests()
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

    private static async Task<(Produto Produto, string IdEstabelecimento)> ConfigurarCenarioAsync(string nomeProduto)
    {
        var (uidConsumidor, _) = await CenarioIntegracao.RegistrarConsumidorAsync(Servicos, EmailUnico("cons"));
        var (uidParceiro, idEstabelecimento) =
            await CenarioIntegracao.ParceiroComEstabelecimentoAsync(Servicos, EmailUnico("parc"));

        var repositorio = Servicos.GetRequiredService<IProdutoRepository>();
        var produto = new Produto
        {
            Nome = nomeProduto,
            Descricao = "Produto de teste",
            PrecoOriginalCentavos = 2000,
            PrecoPromocionalCentavos = 1000,
            QuantidadeDisponivel = 5,
            DataLimite = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(3)),
            Categoria = CategoriaProduto.Mercearia.ToFirestoreString(),
            Status = StatusProduto.Disponivel.ToFirestoreString(),
            EhVegano = false,
            EhSemGluten = false,
            EhSemLactose = true,
            EstabelecimentoId = idEstabelecimento,
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        await repositorio.CriarAsync(produto);

        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();
        await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uidConsumidor, 2);

        return (produto, idEstabelecimento);
    }

    [SkippableFact]
    public async Task Administracao_ListagemUsuarios_ComPaginacao()
    {
        EmuladorGuarda.Validar();

        await ConfigurarCenarioAsync("pao integral");
        await CenarioIntegracao.RegistrarConsumidorAsync(Servicos, EmailUnico("cons"));

        var usuarios = Servicos.GetRequiredService<IUsuarioService>();

        var todos = await usuarios.ObterTodosAsync(1, 500);
        var total = await usuarios.ContarTodosAsync();

        Assert.True(todos.Count <= total && todos.Count >= 3);
        Assert.All(todos, u => Assert.False(string.IsNullOrWhiteSpace(u.Uid)));
        Assert.All(todos, u => Assert.False(u.Perfil == PerfilUsuario.Administrador.ToString()));
    }

    [SkippableFact]
    public async Task Administracao_ListagemEstabelecimentos_ComPaginacao()
    {
        EmuladorGuarda.Validar();

        var (_, idEstabelecimento) = await ConfigurarCenarioAsync("queijo");

        var estabelecimentos = Servicos.GetRequiredService<IEstabelecimentoService>();

        var todos = await estabelecimentos.ObterTodosAsync(1, 500);
        var total = await estabelecimentos.ContarTodosAsync();

        Assert.True(todos.Count <= total && todos.Count >= 1);
        Assert.Contains(todos, e => e.Id == idEstabelecimento);
        Assert.All(todos, e => Assert.False(string.IsNullOrWhiteSpace(e.UsuarioResponsavelId)));
    }

    [SkippableFact]
    public async Task Administracao_ListagemProdutos_ComPaginacao()
    {
        EmuladorGuarda.Validar();

        var (produto, _) = await ConfigurarCenarioAsync("marmita");

        var produtos = Servicos.GetRequiredService<IProdutoService>();
        var filtro = new ProdutoFiltroViewModel { Pagina = 1, TamanhoPagina = 500 };

        var total = await produtos.ContarProdutosDisponiveisAsync(filtro);
        var todos = await produtos.ObterProdutosDisponiveisAsync(filtro);

        Assert.True(todos.Count <= total && todos.Count >= 1);
        Assert.All(todos, p => Assert.False(string.IsNullOrWhiteSpace(p.Id)));
        var noCatalogo = Assert.Single(todos, p => p.Id == produto.Id);
        Assert.Equal(CategoriaProduto.Mercearia, noCatalogo.Categoria);
    }

    [SkippableFact]
    public async Task Administracao_ListagemSolicitacoes_ComPaginacao()
    {
        EmuladorGuarda.Validar();

        await ConfigurarCenarioAsync("sanduiche");

        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        var todas = await solicitacoes.ObterTodasAsync(1, 500);
        var total = await solicitacoes.ContarTodasAsync();

        Assert.True(todas.Count <= total && todas.Count >= 1);
        Assert.All(todas, s => Assert.True(s.Quantidade > 0));
    }

    [SkippableFact]
    public async Task Administracao_AprovacaoDeEstabelecimento_Persiste()
    {
        EmuladorGuarda.Validar();

        var (_, idEstabelecimento) =
            await CenarioIntegracao.ParceiroComEstabelecimentoAsync(Servicos, EmailUnico("parc"));

        var repositorio = Servicos.GetRequiredService<IEstabelecimentoRepository>();
        var estabelecimento = await repositorio.ObterPorIdAsync(idEstabelecimento);
        Assert.NotNull(estabelecimento);
        Assert.False(estabelecimento!.Aprovado);

        estabelecimento.Aprovado = true;
        await repositorio.AtualizarAsync(estabelecimento);

        var aprovado = await repositorio.ObterPorIdAsync(idEstabelecimento);
        Assert.NotNull(aprovado);
        Assert.True(aprovado!.Aprovado);
    }

    private static async Task<(string Uid, string Token)> RegistrarAuthNoEmuladorAsync(string email)
    {
        var contas = Servicos.GetRequiredService<IAuthGateway>();
        var validador = Servicos.GetRequiredService<IFirebaseTokenValidator>();

        var credenciais = await contas.RegistrarComSenhaAsync(email, "Senha123!");
        var validacao = await validador.ValidarAsync(credenciais.IdToken!);
        Assert.True(validacao.Valido, validacao.Erro);

        return (validacao.Uid!, credenciais.IdToken!);
    }

    private static async Task<(string Uid, string Token)> RegistrarAdminNoEmuladorAsync(string email)
    {
        var (adminUid, token) = await RegistrarAuthNoEmuladorAsync(email);

        var dbAdmin = CriarDbAdmin();
        await dbAdmin.Collection("usuarios").Document(adminUid).SetAsync(new
        {
            uid = adminUid,
            nome = "Administrador Teste",
            email,
            perfil = PerfilUsuario.Administrador.ToString(),
            ativo = true,
            criadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        });

        return (adminUid, token);
    }

    private static async Task<(string Uid, string Token)> RegistrarUsuarioNoEmuladorAsync(string nome, string email, string perfil)
    {
        var (uid, token) = await RegistrarAuthNoEmuladorAsync(email);

        var criou = await EmuladorRest.CriarAsync(
            token, "usuarios", uid,
            EmuladorRest.CamposUsuario(uid, nome, email, perfil));
        Assert.Equal(HttpStatusCode.OK, criou.StatusCode);

        return (uid, token);
    }

    private static bool ObtemValorBool(string json, string campo)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("fields")
            .GetProperty(campo)
            .GetProperty("booleanValue")
            .GetBoolean();
    }

    [SkippableFact]
    public async Task Regras_Administrador_LeUsuariosLogs_EAprovaEstabelecimento()
    {
        EmuladorGuarda.Validar();

        var emailAdmin = EmailUnico("admin");
        var (adminUid, tokenAdmin) = await RegistrarAdminNoEmuladorAsync(emailAdmin);

        var (consumidorUid, _) = await RegistrarUsuarioNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (parceiroUid, tokenParceiro) = await RegistrarUsuarioNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());

        var idEstabelecimento = "estab-adm-" + Guid.NewGuid().ToString("N");
        var idProduto = "prod-adm-" + Guid.NewGuid().ToString("N");
        var idLog = "log-adm-" + Guid.NewGuid().ToString("N");

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenParceiro, "estabelecimentos", idEstabelecimento,
                EmuladorRest.CamposEstabelecimento(parceiroUid, "Padaria da Ana"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenParceiro, "produtos", idProduto,
                EmuladorRest.CamposProduto(idEstabelecimento, 10))).StatusCode);

        var dbAdmin = CriarDbAdmin();
        await dbAdmin.Collection("logs").Document(idLog).SetAsync(new
        {
            usuarioId = adminUid,
            acao = "Criar",
            entidade = "Estabelecimento",
            entidadeId = idEstabelecimento,
            resultado = "Sucesso",
            criadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        });

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterAsync(tokenAdmin, "usuarios", adminUid)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterAsync(tokenAdmin, "usuarios", consumidorUid)).StatusCode);

        var aprovou = await EmuladorRest.AtualizarAsync(
            tokenAdmin, "estabelecimentos", idEstabelecimento,
            new Dictionary<string, object> { ["aprovado"] = true });
        Assert.Equal(HttpStatusCode.OK, aprovou.StatusCode);

        var leitura = await EmuladorRest.ObterAsync(tokenParceiro, "estabelecimentos", idEstabelecimento);
        Assert.Equal(HttpStatusCode.OK, leitura.StatusCode);
        Assert.True(ObtemValorBool(await leitura.Content.ReadAsStringAsync(), "aprovado"));

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterAsync(tokenAdmin, "logs", idLog)).StatusCode);
    }

    [SkippableFact]
    public async Task Regras_Consumidor_NaoLeUsuarios_NemLogs()
    {
        EmuladorGuarda.Validar();

        var (consumidorUid, tokenConsumidor) = await RegistrarUsuarioNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (outroUid, _) = await RegistrarUsuarioNoEmuladorAsync(
            "Consumidor B", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var dbAdmin = CriarDbAdmin();
        var idLog = "log-adm-" + Guid.NewGuid().ToString("N");
        await dbAdmin.Collection("logs").Document(idLog).SetAsync(new
        {
            usuarioId = outroUid,
            acao = "Login",
            entidade = "Usuario",
            entidadeId = outroUid,
            resultado = "Sucesso",
            criadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        });

        Assert.Equal(HttpStatusCode.Forbidden,
            (await EmuladorRest.ObterAsync(tokenConsumidor, "usuarios", outroUid)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await EmuladorRest.ObterAsync(tokenConsumidor, "logs", idLog)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.ObterAsync(tokenConsumidor, "usuarios", consumidorUid)).StatusCode);
    }

    [SkippableFact]
    public async Task Regras_AutoCadastro_NaoPodeSeRegistrarComoAdministrador()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarUsuarioNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var camposAdmin = EmuladorRest.CamposUsuario(
            uid, "Falso Admin", EmailUnico("cons"), PerfilUsuario.Administrador.ToString());

        var criouAdmin = await EmuladorRest.CriarAsync(token, "usuarios", uid, camposAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, criouAdmin.StatusCode);

        var promoveu = await EmuladorRest.AtualizarAsync(
            token, "usuarios", uid,
            new Dictionary<string, object> { ["perfil"] = PerfilUsuario.Administrador.ToString() });
        Assert.Equal(HttpStatusCode.Forbidden, promoveu.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_Proprietario_NaoAlteraAprovado_DoSeuEstabelecimento()
    {
        EmuladorGuarda.Validar();

        var (parceiroUid, tokenParceiro) = await RegistrarUsuarioNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());

        var idEstabelecimento = "estab-adm-" + Guid.NewGuid().ToString("N");

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenParceiro, "estabelecimentos", idEstabelecimento,
                EmuladorRest.CamposEstabelecimento(parceiroUid, "Padaria da Ana"))).StatusCode);

        var autoAprovou = await EmuladorRest.AtualizarAsync(
            tokenParceiro, "estabelecimentos", idEstabelecimento,
            new Dictionary<string, object> { ["aprovado"] = true });
        Assert.Equal(HttpStatusCode.Forbidden, autoAprovou.StatusCode);
    }
}