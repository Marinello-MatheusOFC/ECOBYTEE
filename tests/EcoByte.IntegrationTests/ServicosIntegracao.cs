using EcoByte.IntegrationTests;
using EcoByte.Web.Firebase;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Repositories;
using EcoByte.Web.Services;
using FirebaseAdmin;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

public static class ServicosIntegracao
{
    private static readonly object _lock = new();
    private static ServiceProvider? _provider;
    private static bool _appAdminInicializado;

    public static FirebaseConfig CriarConfigAuth()
        => new()
        {
            Enabled = true,
            ProjectId = AmbienteEmulador.Projeto,
            ApiKey = "chave-do-emulador",
            UseEmulator = true,
            AuthEmulatorHost = AmbienteEmulador.HostAuth
        };

    public static ServiceProvider Provider()
    {
        if (_provider is not null)
            return _provider;

        lock (_lock)
        {
            if (_provider is not null)
                return _provider;

            AmbienteEmulador.ConfigurarAmbienteSeguro();
            InicializarAppAdmin();

            var servicos = new ServiceCollection();
            servicos.AddLogging();
            servicos.AddHttpClient("FirebaseAuth", client =>
                client.Timeout = TimeSpan.FromSeconds(20));

            var db = new FirestoreDbBuilder
            {
                ProjectId = AmbienteEmulador.Projeto,
                EmulatorDetection = EmulatorDetection.EmulatorOnly
            }.Build();

            servicos.AddSingleton(db);
            servicos.AddSingleton(CriarConfigAuth());

            servicos.AddSingleton<IAuthGateway, AuthGateway>();
            servicos.AddSingleton<IFirebaseTokenValidator, FirebaseAdminTokenValidator>();

            servicos.AddSingleton<IUsuarioRepository, UsuarioRepository>();
            servicos.AddSingleton<IUsuarioService, UsuarioService>();
            servicos.AddSingleton<IEstabelecimentoRepository, EstabelecimentoRepository>();
            servicos.AddSingleton<IEstabelecimentoService, EstabelecimentoService>();
            servicos.AddSingleton<IProdutoRepository, ProdutoRepository>();
            servicos.AddSingleton<IProdutoService, ProdutoService>();
            servicos.AddSingleton<ISolicitacaoRepository, SolicitacaoRepository>();
            servicos.AddSingleton<ILogService, LogRepository>();
            servicos.AddSingleton<ISolicitacaoService, SolicitacaoService>();

            servicos.AddSingleton<IAutenticacaoService, AutenticacaoService>();

            _provider = servicos.BuildServiceProvider();
            return _provider;
        }
    }

    private static void InicializarAppAdmin()
    {
        if (_appAdminInicializado)
            return;

        FirebaseApp? app;
        try
        {
            app = FirebaseApp.DefaultInstance;
        }
        catch
        {
            app = null;
        }

        if (app is null)
        {
            try
            {
                FirebaseApp.Create(new AppOptions
                {
                    ProjectId = AmbienteEmulador.ProjetoAuthAdmin,
                    Credential = GoogleCredential.FromAccessToken("owner")
                });
            }
            catch (ArgumentException)
            {
            }
        }

        _appAdminInicializado = true;
    }
}