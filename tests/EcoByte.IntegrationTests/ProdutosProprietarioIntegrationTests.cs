using System.Net;
using EcoByte.IntegrationTests;
using EcoByte.Web.Enums;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.ViewModels;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

public class ProdutosProprietarioIntegrationTests
{
    static ProdutosProprietarioIntegrationTests()
    {
        AmbienteEmulador.ConfigurarAmbienteSeguro();
    }

    private static ServiceProvider Servicos => ServicosIntegracao.Provider();

    private static string EmailUnico()
        => $"produto-{Guid.NewGuid():N}@exemplo.com";

    private static Task<(string Uid, string EstabelecimentoId)> CriarParceiroComEstabelecimentoAsync(
        string email)
        => CenarioIntegracao.ParceiroComEstabelecimentoAsync(Servicos, email);

    private static async Task<Produto> CriarProdutoAsync(string estabelecimentoId, string nome, StatusProduto status, int quantidade)
    {
        var repositorio = Servicos.GetRequiredService<IProdutoRepository>();

        var produto = new Produto
        {
            Nome = $"{nome} {Guid.NewGuid():N}",
            Descricao = "Produto de teste",
            PrecoOriginalCentavos = 1000,
            PrecoPromocionalCentavos = 500,
            QuantidadeDisponivel = quantidade,
            DataLimite = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(3)),
            Categoria = CategoriaProduto.Padaria.ToFirestoreString(),
            Status = status.ToFirestoreString(),
            EhVegano = true,
            EhSemGluten = true,
            EhSemLactose = false,
            EstabelecimentoId = estabelecimentoId,
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        await repositorio.CriarAsync(produto);
        return produto;
    }

    private static ProdutoFiltroViewModel FiltroComTermo(string termo)
        => new()
        {
            Termo = termo,
            Pagina = 1,
            TamanhoPagina = 50
        };

    [SkippableFact]
    public async Task ProdutoDisponivel_ComEstoque_EdataValida_ApareceNoCatalogo()
    {
        EmuladorGuarda.Validar();

        var (_, estabelecimentoId) = await CriarParceiroComEstabelecimentoAsync(EmailUnico());
        var criado = await CriarProdutoAsync(
            estabelecimentoId, "pao integral", StatusProduto.Disponivel, 10);

        var servico = Servicos.GetRequiredService<IProdutoService>();

        var catalogo = await servico.ObterProdutosDisponiveisAsync(FiltroComTermo(criado.Nome));
        var detalhes = await servico.ObterDetalhesAsync(criado.Id!);

        Assert.Single(catalogo);
        Assert.Equal(criado.Id, catalogo[0].Id);
        Assert.NotNull(detalhes);
        Assert.Equal(criado.Nome, detalhes!.Nome);
        Assert.Equal(5.00m, detalhes.PrecoPromocional);
        Assert.Equal(50, detalhes.PercentualDesconto);
        Assert.Equal(10, detalhes.QuantidadeDisponivel);
    }

    [SkippableFact]
    public async Task ProdutoRascunho_OuDesativado_NaoApareceNoCatalogo()
    {
        EmuladorGuarda.Validar();

        var (_, estabelecimentoId) = await CriarParceiroComEstabelecimentoAsync(EmailUnico());
        var rascunho = await CriarProdutoAsync(
            estabelecimentoId, "bolo", StatusProduto.Rascunho, 5);
        var desativado = await CriarProdutoAsync(
            estabelecimentoId, "torta", StatusProduto.Desativado, 5);

        var servico = Servicos.GetRequiredService<IProdutoService>();

        Assert.Empty(await servico.ObterProdutosDisponiveisAsync(FiltroComTermo(rascunho.Nome)));
        Assert.Empty(await servico.ObterProdutosDisponiveisAsync(FiltroComTermo(desativado.Nome)));
    }

    [SkippableFact]
    public async Task ProdutoSemEstoque_OuDataLimitePassado_NaoApareceNoCatalogo()
    {
        EmuladorGuarda.Validar();

        var (_, estabelecimentoId) = await CriarParceiroComEstabelecimentoAsync(EmailUnico());
        var semEstoque = await CriarProdutoAsync(
            estabelecimentoId, "coxinha", StatusProduto.Disponivel, 0);

        var vendido = await CriarProdutoAsync(
            estabelecimentoId, "quibe", StatusProduto.Esgotado, 0);

        var repositorio = Servicos.GetRequiredService<IProdutoRepository>();
        var vencido = new Produto
        {
            Nome = $"sanduiche {Guid.NewGuid():N}",
            Descricao = "Teste",
            PrecoOriginalCentavos = 1000,
            PrecoPromocionalCentavos = 400,
            QuantidadeDisponivel = 5,
            DataLimite = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(-1)),
            Categoria = CategoriaProduto.Outros.ToFirestoreString(),
            Status = StatusProduto.Disponivel.ToFirestoreString(),
            EstabelecimentoId = estabelecimentoId,
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        await repositorio.CriarAsync(vencido);

        var servico = Servicos.GetRequiredService<IProdutoService>();

        Assert.Empty(await servico.ObterProdutosDisponiveisAsync(FiltroComTermo(semEstoque.Nome)));
        Assert.Empty(await servico.ObterProdutosDisponiveisAsync(FiltroComTermo(vendido.Nome)));
        Assert.Empty(await servico.ObterProdutosDisponiveisAsync(FiltroComTermo(vencido.Nome)));
    }

    [SkippableFact]
    public async Task Filtros_CategoriaTermo_EAtributosSaoAplicados()
    {
        EmuladorGuarda.Validar();

        var (_, estabelecimentoId) = await CriarParceiroComEstabelecimentoAsync(EmailUnico());
        await CriarProdutoAsync(estabelecimentoId, "pao integral", StatusProduto.Disponivel, 10);
        await CriarProdutoAsync(estabelecimentoId, "queijo minas", StatusProduto.Disponivel, 5);

        var servico = Servicos.GetRequiredService<IProdutoService>();

        var porTermo = await servico.ObterProdutosDisponiveisAsync(new ProdutoFiltroViewModel
        {
            Termo = "queijo",
            Pagina = 1,
            TamanhoPagina = 50
        });

        Assert.All(porTermo, p => Assert.StartsWith("queijo", p.Nome, StringComparison.OrdinalIgnoreCase));

        var porCategoria = await servico.ObterProdutosDisponiveisAsync(new ProdutoFiltroViewModel
        {
            Categoria = CategoriaProduto.Padaria,
            Pagina = 1,
            TamanhoPagina = 50
        });

        Assert.NotEmpty(porCategoria);
        Assert.All(porCategoria, p => Assert.Equal(CategoriaProduto.Padaria, p.Categoria));

        var somenteVeganosESemGluten = await servico.ObterProdutosDisponiveisAsync(new ProdutoFiltroViewModel
        {
            SomenteVeganos = true,
            SomenteSemGluten = true,
            Pagina = 1,
            TamanhoPagina = 50
        });

        Assert.NotEmpty(somenteVeganosESemGluten);
        Assert.All(somenteVeganosESemGluten, p => Assert.True(p.EhVegano && p.EhSemGluten));
    }

    [SkippableFact]
    public async Task ProdutosDaEmpresa_SaoEscopadosPorEstabelecimento()
    {
        EmuladorGuarda.Validar();

        var (_, primeiro) = await CriarParceiroComEstabelecimentoAsync(EmailUnico());
        var (_, segundo) = await CriarParceiroComEstabelecimentoAsync(EmailUnico());

        var produtoDoPrimeiro = await CriarProdutoAsync(primeiro, "croissant", StatusProduto.Disponivel, 3);

        var repositorio = Servicos.GetRequiredService<IProdutoRepository>();

        var doPrimeiro = await repositorio.ObterPorEstabelecimentoAsync(primeiro, 50, 0);
        var doSegundo = await repositorio.ObterPorEstabelecimentoAsync(segundo, 50, 0);

        Assert.Contains(doPrimeiro, p => p.Id == produtoDoPrimeiro.Id);
        Assert.DoesNotContain(doSegundo, p => p.Id == produtoDoPrimeiro.Id);
    }

    [SkippableFact]
    public async Task Estabelecimento_DuplicadoParaOMesmoUsuario_Rejeitado()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var (uid, primeiro) = await CriarParceiroComEstabelecimentoAsync(email);

        var estabelecimentos = Servicos.GetRequiredService<IEstabelecimentoService>();

        var existe = await estabelecimentos.ObterPorUsuarioAsync(uid);
        Assert.Equal(primeiro, existe!.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            estabelecimentos.CriarAsync(
                uid, "Segundo Comercio", "(11) 96666-0000", email, "Outro", "Rua C"));
    }

    [SkippableFact]
    public async Task Regras_Proprietario_CriaEAtualizaProdutoDoSeuEstabelecimento()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();
        var contas = Servicos.GetRequiredService<IAuthGateway>();

        var registro = await autenticacao.RegistrarAsync("Dona Maria", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);

        var uid = registro.Uid!;
        var token = (await contas.AutenticarComSenhaAsync(email, "Senha123!")).IdToken!;
        var idEstabelecimento = "estab-rules-" + Guid.NewGuid().ToString("N");
        var idProduto = "prod-rules-" + Guid.NewGuid().ToString("N");

        var criouUsuario = await EmuladorRest.CriarAsync(
            token, "usuarios", uid,
            EmuladorRest.CamposUsuario(uid, "Dona Maria", email, PerfilUsuario.Parceiro.ToString()));
        Assert.Equal(HttpStatusCode.OK, criouUsuario.StatusCode);

        var criouEstabelecimento = await EmuladorRest.CriarAsync(
            token, "estabelecimentos", idEstabelecimento,
            EmuladorRest.CamposEstabelecimento(uid, "Padaria da Maria"));
        Assert.Equal(HttpStatusCode.OK, criouEstabelecimento.StatusCode);

        var criouProduto = await EmuladorRest.CriarAsync(
            token, "produtos", idProduto,
            EmuladorRest.CamposProduto(idEstabelecimento, 10));
        Assert.Equal(HttpStatusCode.OK, criouProduto.StatusCode);

        var atualizouProduto = await EmuladorRest.AtualizarAsync(
            token, "produtos", idProduto,
            new Dictionary<string, object> { ["quantidadeDisponivel"] = 7 });
        Assert.Equal(HttpStatusCode.OK, atualizouProduto.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_OutroParceiro_NaoConsegueCriarOuAtualizarProdutoAlheio()
    {
        EmuladorGuarda.Validar();

        var emailA = EmailUnico();
        var emailB = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();
        var contas = Servicos.GetRequiredService<IAuthGateway>();

        var a = await autenticacao.RegistrarAsync("Dono A", emailA, "Senha123!");
        var b = await autenticacao.RegistrarAsync("Dono B", emailB, "Senha123!");
        Assert.True(a.Sucesso, a.Erro);
        Assert.True(b.Sucesso, b.Erro);

        var tokenA = (await contas.AutenticarComSenhaAsync(emailA, "Senha123!")).IdToken!;
        var tokenB = (await contas.AutenticarComSenhaAsync(emailB, "Senha123!")).IdToken!;

        var idEstabelecimento = "estab-rules-" + Guid.NewGuid().ToString("N");
        var idProduto = "prod-rules-" + Guid.NewGuid().ToString("N");

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenA, "usuarios", a.Uid!,
                EmuladorRest.CamposUsuario(a.Uid!, "Dono A", emailA, PerfilUsuario.Parceiro.ToString()))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenA, "estabelecimentos", idEstabelecimento,
                EmuladorRest.CamposEstabelecimento(a.Uid!, "Comercio do A"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenA, "produtos", idProduto,
                EmuladorRest.CamposProduto(idEstabelecimento, 10))).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(tokenB, "usuarios", b.Uid!,
                EmuladorRest.CamposUsuario(b.Uid!, "Dono B", emailB, PerfilUsuario.Parceiro.ToString()))).StatusCode);

        var criouAlheio = await EmuladorRest.CriarAsync(
            tokenB, "produtos", "prod-alheio-" + Guid.NewGuid().ToString("N"),
            EmuladorRest.CamposProduto(idEstabelecimento, 5));
        Assert.Equal(HttpStatusCode.Forbidden, criouAlheio.StatusCode);

        var atualizouAlheio = await EmuladorRest.AtualizarAsync(
            tokenB, "produtos", idProduto,
            new Dictionary<string, object> { ["quantidadeDisponivel"] = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, atualizouAlheio.StatusCode);
    }
}