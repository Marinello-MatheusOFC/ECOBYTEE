using EcoByte.IntegrationTests;
using EcoByte.Web.Enums;
using EcoByte.Web.Interfaces;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

public class PerfisAutorizacaoIntegrationTests
{
    static PerfisAutorizacaoIntegrationTests()
    {
        AmbienteEmulador.ConfigurarAmbienteSeguro();
    }

    private static ServiceProvider Servicos => ServicosIntegracao.Provider();

    private static string EmailUnico()
        => $"perfil-{Guid.NewGuid():N}@exemplo.com";

    [SkippableFact]
    public async Task Onboarding_Ong_DefinirPerfilOng_Persiste_ELogaComPerfil()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();

        var registro = await autenticacao.RegistrarAsync("ONG Vida", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), registro.Perfil);

        await Servicos.GetRequiredService<IUsuarioService>()
            .DefinirPerfilAsync(registro.Uid!, email, "ONG Vida", PerfilUsuario.Ong.ToString());

        var db = Servicos.GetRequiredService<FirestoreDb>();
        var snapshot = await db.Collection("usuarios").Document(registro.Uid!).GetSnapshotAsync();

        Assert.True(snapshot.Exists);
        Assert.Equal(PerfilUsuario.Ong.ToString(), snapshot.GetValue<string>("perfil"));

        var login = await autenticacao.LoginAsync(email, "Senha123!");

        Assert.True(login.Sucesso, login.Erro);
        Assert.Equal(PerfilUsuario.Ong.ToString(), login.Perfil);
        Assert.Null(login.EstabelecimentoId);
    }

    [SkippableFact]
    public async Task Onboarding_MudancaDePerfil_AtualizaDocumento_ULtimoPerfilVencer()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();
        var usuarios = Servicos.GetRequiredService<IUsuarioService>();

        var registro = await autenticacao.RegistrarAsync("Pedro", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);

        await usuarios.DefinirPerfilAsync(registro.Uid!, email, "Pedro", PerfilUsuario.Parceiro.ToString());
        await usuarios.DefinirPerfilAsync(registro.Uid!, email, "Pedro", PerfilUsuario.Ong.ToString());

        var usuario = await usuarios.ObterPorUidAsync(registro.Uid!);

        Assert.NotNull(usuario);
        Assert.Equal(PerfilUsuario.Ong.ToString(), usuario.Perfil);

        var login = await autenticacao.LoginAsync(email, "Senha123!");

        Assert.True(login.Sucesso, login.Erro);
        Assert.Equal(PerfilUsuario.Ong.ToString(), login.Perfil);
    }

    [SkippableFact]
    public async Task Login_ContaSemPerfilNoFirestore_RetornaSucessoSemPerfil()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();
        var gateway = Servicos.GetRequiredService<IAuthGateway>();

        var conta = await gateway.RegistrarComSenhaAsync(email, "Senha123!");
        Assert.True(conta.Sucesso, conta.Erro);

        var login = await autenticacao.LoginAsync(email, "Senha123!");

        Assert.True(login.Sucesso, login.Erro);
        Assert.False(string.IsNullOrWhiteSpace(login.Uid));
        Assert.False(login.PossuiPerfil);
        Assert.Null(login.Perfil);
        Assert.Null(login.EstabelecimentoId);
    }

    [SkippableFact]
    public async Task DefinirPerfil_Repetido_NaoCriaDuplicidade()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var uid = "uid-perfil-" + Guid.NewGuid().ToString("N");
        var usuarios = Servicos.GetRequiredService<IUsuarioService>();

        var primeiro = await usuarios.ObouCriarAsync(uid, "Carla", email);

        await usuarios.DefinirPerfilAsync(uid, email, "Carla", PerfilUsuario.Parceiro.ToString());
        await usuarios.DefinirPerfilAsync(uid, email, "Carla", PerfilUsuario.Parceiro.ToString());

        var db = Servicos.GetRequiredService<FirestoreDb>();
        var snapshot = await db.Collection("usuarios")
            .WhereEqualTo("uid", uid)
            .GetSnapshotAsync();

        Assert.Single(snapshot.Documents);
        Assert.Equal(PerfilUsuario.Parceiro.ToString(), snapshot.Documents[0].GetValue<string>("perfil"));
        Assert.Equal(primeiro.Id, snapshot.Documents[0].Id);
    }

    [SkippableFact]
    public async Task Login_RestauraPerfilEestabelecimento_ParaParceiro()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();
        var usuarios = Servicos.GetRequiredService<IUsuarioService>();
        var estabelecimentos = Servicos.GetRequiredService<IEstabelecimentoService>();

        var registro = await autenticacao.RegistrarAsync("Padaria Boa", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);

        await usuarios.DefinirPerfilAsync(registro.Uid!, email, "Padaria Boa", PerfilUsuario.Parceiro.ToString());

        var idEstabelecimento = await estabelecimentos.CriarAsync(
            registro.Uid!, "Padaria Boa", "(11) 95555-0000", email, "Padaria", "Rua X, 10");

        var primeiroLogin = await autenticacao.LoginAsync(email, "Senha123!");
        Assert.True(primeiroLogin.Sucesso, primeiroLogin.Erro);
        Assert.Equal(idEstabelecimento, primeiroLogin.EstabelecimentoId);

        var segundoLogin = await autenticacao.LoginAsync(email, "Senha123!");
        Assert.True(segundoLogin.Sucesso, segundoLogin.Erro);
        Assert.Equal(PerfilUsuario.Parceiro.ToString(), segundoLogin.Perfil);
        Assert.Equal(idEstabelecimento, segundoLogin.EstabelecimentoId);
    }
}