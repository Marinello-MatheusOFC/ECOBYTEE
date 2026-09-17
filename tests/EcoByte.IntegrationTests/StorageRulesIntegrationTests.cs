using System.Net;
using EcoByte.IntegrationTests;
using EcoByte.Web.Enums;
using EcoByte.Web.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public class StorageRulesIntegrationTests
{
    static StorageRulesIntegrationTests()
    {
        AmbienteEmulador.ConfigurarAmbienteSeguro();
    }

    private static ServiceProvider Servicos => ServicosIntegracao.Provider();

    private static string EmailUnico(string prefixo)
        => $"{prefixo}-{Guid.NewGuid():N}@exemplo.com";

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

    private static bool FoiNegado(HttpStatusCode status)
        => status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    [SkippableFact]
    public async Task Regras_Storage_Avatar_SomenteOPaiPodeEscrever()
    {
        EmuladorGuarda.Validar();

        var (uidA, tokenA) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (_, tokenB) = await RegistrarNoEmuladorAsync(
            "Consumidor B", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var caminho = $"usuarios/{uidA}/avatar.png";

        var donoEnviou = await EmuladorStorage.EnviarPngAsync(tokenA, caminho);
        Assert.Equal(HttpStatusCode.OK, donoEnviou.StatusCode);

        var outroEnviou = await EmuladorStorage.EnviarPngAsync(tokenB, caminho);
        Assert.True(FoiNegado(outroEnviou.StatusCode));

        var anonimoEnviou = await EmuladorStorage.EnviarPngAsync(string.Empty, caminho);
        Assert.True(FoiNegado(anonimoEnviou.StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Storage_Avatar_RejeitaNaoImagem_EArquivoAcimaDe5Mb()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var caminho = $"usuarios/{uid}/avatar.png";

        var texto = await EmuladorStorage.EnviarTextoAsync(token, caminho);
        Assert.True(FoiNegado(texto.StatusCode));

        var grande = await EmuladorStorage.EnviarPngAsync(token, caminho, 6 * 1024 * 1024);
        Assert.True(FoiNegado(grande.StatusCode));

        var valido = await EmuladorStorage.EnviarPngAsync(token, caminho);
        Assert.Equal(HttpStatusCode.OK, valido.StatusCode);
    }

    [SkippableFact]
    public async Task Regras_Storage_Avatar_ExclusaoSomenteDono()
    {
        EmuladorGuarda.Validar();

        var (uid, tokenDono) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());
        var (_, tokenOutro) = await RegistrarNoEmuladorAsync(
            "Consumidor B", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var caminho = $"usuarios/{uid}/avatar.png";
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorStorage.EnviarPngAsync(tokenDono, caminho)).StatusCode);

        var outroExcluiu = await EmuladorStorage.ExcluirAsync(tokenOutro, caminho);
        Assert.True(FoiNegado(outroExcluiu.StatusCode));

        var anonimoExcluiu = await EmuladorStorage.ExcluirAsync(string.Empty, caminho);
        Assert.True(FoiNegado(anonimoExcluiu.StatusCode));

        var donoExcluiu = await EmuladorStorage.ExcluirAsync(tokenDono, caminho);
        Assert.True(donoExcluiu.IsSuccessStatusCode);
    }

    [SkippableFact]
    public async Task Regras_Storage_CaminhoArbitrario_Bloqueado()
    {
        EmuladorGuarda.Validar();

        var (uid, token) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var foraDoAvatar = await EmuladorStorage.EnviarPngAsync(token, $"usuarios/{uid}/outro.png");
        Assert.True(FoiNegado(foraDoAvatar.StatusCode));

        var qualquerCaminho = await EmuladorStorage.EnviarPngAsync(token, "qualquer/coisa/foto.png");
        Assert.True(FoiNegado(qualquerCaminho.StatusCode));

        var caminhoLegadoProduto = await EmuladorStorage.EnviarPngAsync(
            token, $"produtos/produto-qualquer/imagens/foto.png");
        Assert.True(FoiNegado(caminhoLegadoProduto.StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Storage_Estabelecimento_UploadResponsavel_Aceito_OutroNegado_SemVinculoNegado()
    {
        Skip.If(true,
            "O runtime de regras do emulador de Storage nao implementa get() cross-service "
            + "(error: 'Function not found error: Name: [get]'), portanto as regras de imagens de "
            + "estabelecimento/produto nao podem ser avaliadas no emulador e negam todas as escritas. "
            + "Validar em projeto real/roteiro manual (docs/validacao-storage-rules.md).");

        EmuladorGuarda.Validar();

        var (parceiroUid, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var (_, tokenOutroParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Bea", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var (_, tokenConsumidor) = await RegistrarNoEmuladorAsync(
            "Consumidor A", EmailUnico("cons"), PerfilUsuario.Consumidor.ToString());

        var idEstabelecimento = "estab-storage-" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(
                tokenParceiro, "estabelecimentos", idEstabelecimento,
                EmuladorRest.CamposEstabelecimento(parceiroUid, "Padaria da Ana"))).StatusCode);

        var caminho = $"estabelecimentos/{idEstabelecimento}/imagens/fachada.png";

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorStorage.EnviarPngAsync(tokenParceiro, caminho)).StatusCode);
        Assert.True(FoiNegado(
            (await EmuladorStorage.EnviarPngAsync(tokenOutroParceiro, caminho)).StatusCode));
        Assert.True(FoiNegado(
            (await EmuladorStorage.EnviarPngAsync(tokenConsumidor, caminho)).StatusCode));
        Assert.True(FoiNegado(
            (await EmuladorStorage.EnviarPngAsync(string.Empty, caminho)).StatusCode));
        Assert.True(FoiNegado(
            (await EmuladorStorage.EnviarTextoAsync(tokenParceiro, caminho)).StatusCode));
    }

    [SkippableFact]
    public async Task Regras_Storage_Estabelecimento_ExclusaoPorNaoAutorizado_Negada()
    {
        Skip.If(true,
            "O runtime de regras do emulador de Storage nao implementa get() cross-service "
            + "(error: 'Function not found error: Name: [get]'). Validar em projeto real/roteiro manual.");

        EmuladorGuarda.Validar();

        var (parceiroUid, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var (_, tokenIntruso) = await RegistrarNoEmuladorAsync(
            "Dona Bea", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());

        var idEstabelecimento = "estab-storage-" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(
                tokenParceiro, "estabelecimentos", idEstabelecimento,
                EmuladorRest.CamposEstabelecimento(parceiroUid, "Padaria da Ana"))).StatusCode);

        var caminho = $"estabelecimentos/{idEstabelecimento}/imagens/fachada.png";
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorStorage.EnviarPngAsync(tokenParceiro, caminho)).StatusCode);

        Assert.True(FoiNegado(
            (await EmuladorStorage.ExcluirAsync(tokenIntruso, caminho)).StatusCode));
        Assert.True(
            (await EmuladorStorage.ExcluirAsync(tokenParceiro, caminho)).IsSuccessStatusCode);
    }

    [SkippableFact]
    public async Task Regras_Storage_Produto_VinculadoAoEstabelecimento()
    {
        Skip.If(true,
            "O runtime de regras do emulador de Storage nao implementa get() cross-service "
            + "(error: 'Function not found error: Name: [get]'). Validar em projeto real/roteiro manual.");

        EmuladorGuarda.Validar();

        var (parceiroUid, tokenParceiro) = await RegistrarNoEmuladorAsync(
            "Dona Ana", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());
        var (_, tokenIntruso) = await RegistrarNoEmuladorAsync(
            "Dona Bea", EmailUnico("parc"), PerfilUsuario.Parceiro.ToString());

        var idEstabelecimento = "estab-storage-" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(
                tokenParceiro, "estabelecimentos", idEstabelecimento,
                EmuladorRest.CamposEstabelecimento(parceiroUid, "Padaria da Ana"))).StatusCode);

        var idProduto = "prod-storage-" + Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorRest.CriarAsync(
                tokenParceiro, "produtos", idProduto,
                EmuladorRest.CamposProduto(idEstabelecimento, 5))).StatusCode);

        var caminho = $"estabelecimentos/{idEstabelecimento}/produtos/{idProduto}/imagens/foto.png";

        Assert.Equal(HttpStatusCode.OK,
            (await EmuladorStorage.EnviarPngAsync(tokenParceiro, caminho)).StatusCode);
        Assert.True(FoiNegado(
            (await EmuladorStorage.EnviarPngAsync(tokenIntruso, caminho)).StatusCode));

        var caminhoProdutoAlheio = $"estabelecimentos/{idEstabelecimento}/produtos/produto-inexistente/imagens/foto.png";
        Assert.True(FoiNegado(
            (await EmuladorStorage.EnviarPngAsync(tokenParceiro, caminhoProdutoAlheio)).StatusCode));
    }
}