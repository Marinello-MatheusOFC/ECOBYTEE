using EcoByte.IntegrationTests;
using EcoByte.Web.Enums;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

public class AutenticacaoOnboardingIntegrationTests
{
    static AutenticacaoOnboardingIntegrationTests()
    {
        AmbienteEmulador.ConfigurarAmbienteSeguro();
    }

    private static ServiceProvider Servicos => ServicosIntegracao.Provider();

    private static string EmailUnico()
        => $"onboarding-{Guid.NewGuid():N}@exemplo.com";

    [SkippableFact]
    public async Task Registrar_FluxoCompleto_CriaUsuarioConsumidor_ELogaNoEmulador()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();

        var resultado = await autenticacao.RegistrarAsync("Ana Teste", email, "Senha123!");

        Assert.True(resultado.Sucesso, resultado.Erro);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Uid));
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), resultado.Perfil);
        Assert.True(resultado.PossuiPerfil);

        var login = await autenticacao.LoginAsync(email, "Senha123!");

        Assert.True(login.Sucesso, login.Erro);
        Assert.Equal(resultado.Uid, login.Uid);
        Assert.True(login.PossuiPerfil);
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), login.Perfil);

        var usuario = await Servicos.GetRequiredService<IUsuarioService>()
            .ObterPorUidAsync(resultado.Uid!);

        Assert.NotNull(usuario);
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), usuario.Perfil);
        Assert.Equal(email, usuario.Email, ignoreCase: true);
    }

    [SkippableFact]
    public async Task Registrar_AutenticacaoService_CriaDocumentoComIdIgualAoUid()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();

        var resultado = await autenticacao.RegistrarAsync("Ana Teste", email, "Senha123!");

        Assert.True(resultado.Sucesso, resultado.Erro);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Uid));
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), resultado.Perfil);
        Assert.Equal("Ana Teste", resultado.Nome);

        var db = Servicos.GetRequiredService<FirestoreDb>();
        var snapshot = await db.Collection("usuarios").Document(resultado.Uid!).GetSnapshotAsync();

        Assert.True(snapshot.Exists);
        Assert.Equal(resultado.Uid, snapshot.Id);
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), snapshot.GetValue<string>("perfil"));
        Assert.True(snapshot.GetValue<bool>("ativo"));
    }

    [SkippableFact]
    public async Task Registrar_EmailDuplicado_RetornaFalha()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();

        var primeiro = await autenticacao.RegistrarAsync("Ana", email, "Senha123!");
        Assert.True(primeiro.Sucesso, primeiro.Erro);

        var segundo = await autenticacao.RegistrarAsync("Ana", email, "Senha123!");

        Assert.False(segundo.Sucesso);
        Assert.NotNull(segundo.Erro);
        Assert.Contains("Ja existe uma conta", segundo.Erro);
    }

    [SkippableFact]
    public async Task Registrar_SenhaCurta_RetornaFalhaSenhaFraca()
    {
        EmuladorGuarda.Validar();

        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();

        var resultado = await autenticacao.RegistrarAsync("Ana", EmailUnico(), "abc12");

        Assert.False(resultado.Sucesso);
        Assert.NotNull(resultado.Erro);
    }

    [SkippableFact]
    public async Task Login_UsuarioDesativado_RetornaFalha()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();

        var registro = await autenticacao.RegistrarAsync("Ana", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);

        var usuario = await Servicos.GetRequiredService<IUsuarioService>()
            .ObterPorUidAsync(registro.Uid!);
        Assert.NotNull(usuario);

        usuario.Ativo = false;
        await Servicos.GetRequiredService<IUsuarioRepository>()
            .AtualizarAsync(usuario);

        var login = await autenticacao.LoginAsync(email, "Senha123!");

        Assert.False(login.Sucesso);
        Assert.Equal("Conta desativada.", login.Erro);
    }

    [SkippableFact]
    public async Task ValidarToken_Real_ComFirebaseAdmin_ReconheceTokenDoEmulador()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var gateway = Servicos.GetRequiredService<IAuthGateway>();

        var registro = await gateway.RegistrarComSenhaAsync(email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);

        var validador = Servicos.GetRequiredService<IFirebaseTokenValidator>();
        var validacao = await validador.ValidarAsync(registro.IdToken);

        Assert.True(validacao.Valido, validacao.Erro);
        Assert.False(string.IsNullOrWhiteSpace(validacao.Uid));
        Assert.Equal(email, validacao.Email, ignoreCase: true);
    }

    [SkippableFact]
    public async Task Onboarding_DefinirPerfilParceiro_CriaEstabelecimento_EretornaNoLogin()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();

        var registro = await autenticacao.RegistrarAsync("Maria", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);
        var uid = registro.Uid!;

        await Servicos.GetRequiredService<IUsuarioService>()
            .DefinirPerfilAsync(uid, email, "Maria", PerfilUsuario.Parceiro.ToString());

        var idEstabelecimento = await Servicos.GetRequiredService<IEstabelecimentoService>()
            .CriarAsync(uid, "Panificadora Teste", "(11) 90000-0000", email, "Padaria", "Rua A, 100");

        Assert.False(string.IsNullOrWhiteSpace(idEstabelecimento));

        var estabelecimento = await Servicos.GetRequiredService<IEstabelecimentoService>()
            .ObterPorUsuarioAsync(uid);

        Assert.NotNull(estabelecimento);
        Assert.Equal(idEstabelecimento, estabelecimento.Id);
        Assert.False(estabelecimento.Aprovado);
        Assert.True(estabelecimento.Ativo);

        var login = await autenticacao.LoginAsync(email, "Senha123!");

        Assert.True(login.Sucesso, login.Erro);
        Assert.Equal(PerfilUsuario.Parceiro.ToString(), login.Perfil);
        Assert.Equal(idEstabelecimento, login.EstabelecimentoId);
    }

    [SkippableFact]
    public async Task Onboarding_DefinirPerfilAdministrador_NaoPermitido()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var autenticacao = Servicos.GetRequiredService<IAutenticacaoService>();

        var registro = await autenticacao.RegistrarAsync("Carlos", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);

        var usuarios = Servicos.GetRequiredService<IUsuarioService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            usuarios.DefinirPerfilAsync(registro.Uid!, email, "Carlos", PerfilUsuario.Administrador.ToString()));

        var usuario = await usuarios.ObterPorUidAsync(registro.Uid!);
        Assert.NotNull(usuario);
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), usuario.Perfil);
    }

    [SkippableFact]
    public async Task Onboarding_ObouCriar_Repetido_NaoDuplicaUsuario()
    {
        EmuladorGuarda.Validar();

        var email = EmailUnico();
        var uid = "uid-obou-" + Guid.NewGuid().ToString("N");
        var usuarios = Servicos.GetRequiredService<IUsuarioService>();

        var primeiro = await usuarios.ObouCriarAsync(uid, "Bruno", email);
        var segundo = await usuarios.ObouCriarAsync(uid, "Bruno", email);

        Assert.Equal(uid, primeiro.Id);
        Assert.Equal(uid, segundo.Id);

        var db = Servicos.GetRequiredService<FirestoreDb>();
        var snapshot = await db.Collection("usuarios")
            .WhereEqualTo("uid", uid)
            .GetSnapshotAsync();

        Assert.Single(snapshot.Documents);
        Assert.Equal(uid, snapshot.Documents[0].Id);
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), snapshot.Documents[0].GetValue<string>("perfil"));
    }
}