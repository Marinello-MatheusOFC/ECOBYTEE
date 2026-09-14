using EcoByte.IntegrationTests;
using EcoByte.Web.Firebase;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Services;
using Microsoft.Extensions.DependencyInjection;

public class FluxoAutenticacaoIntegrationTests
{
    private static FirebaseConfig CriarConfig()
        => new()
        {
            Enabled = true,
            ProjectId = AmbienteEmulador.Projeto,
            ApiKey = "chave-do-emulador",
            UseEmulator = true,
            AuthEmulatorHost = AmbienteEmulador.HostAuth
        };

    [SkippableFact]
    public async Task Registrar_Erautenticar_FluxoCompletoNoEmulador()
    {
        EmuladorGuarda.Validar();

        var config = CriarConfig();

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddHttpClient("FirebaseAuth", client => client.Timeout = TimeSpan.FromSeconds(15));
        using var provider = servicos.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var gateway = new AuthGateway(config, factory);

        var email = $"integracao-{Guid.NewGuid():N}@exemplo.com";
        var senha = "Senha123!";

        var registro = await gateway.RegistrarComSenhaAsync(email, senha);

        Assert.True(registro.Sucesso, registro.Erro);
        Assert.False(string.IsNullOrWhiteSpace(registro.IdToken));
        Assert.Equal(email, registro.Email, ignoreCase: true);

        var login = await gateway.AutenticarComSenhaAsync(email, senha);
        Assert.True(login.Sucesso, login.Erro);
        Assert.False(string.IsNullOrWhiteSpace(login.IdToken));
    }

    [SkippableFact]
    public async Task Autenticar_SenhaIncorreta_RetornaFalha()
    {
        EmuladorGuarda.Validar();

        var config = CriarConfig();

        var servicos = new ServiceCollection();
        servicos.AddLogging();
        servicos.AddHttpClient("FirebaseAuth", client => client.Timeout = TimeSpan.FromSeconds(15));
        using var provider = servicos.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var gateway = new AuthGateway(config, factory);

        var email = $"integracao-{Guid.NewGuid():N}@exemplo.com";
        await gateway.RegistrarComSenhaAsync(email, "Senha123!");

        var login = await gateway.AutenticarComSenhaAsync(email, "OutraSenha1");

        Assert.False(login.Sucesso);
        Assert.NotNull(login.Erro);
    }
}