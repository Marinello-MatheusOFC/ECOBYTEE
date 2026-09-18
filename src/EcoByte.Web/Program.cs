using EcoByte.Web.Firebase;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Repositories;
using EcoByte.Web.Security;
using EcoByte.Web.Services;
using FirebaseAdmin;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<FirebaseConfig>(
    builder.Configuration.GetSection("Firebase"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Logout";
        options.AccessDeniedPath = "/Conta/AcessoNegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = false;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.Autenticado, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(AuthPolicies.Consumidor, policy => policy.RequireRole(AuthPolicies.PerfilConsumidor));
    options.AddPolicy(AuthPolicies.Parceiro, policy => policy.RequireRole(AuthPolicies.PerfilParceiro));
    options.AddPolicy(AuthPolicies.Ong, policy => policy.RequireRole(AuthPolicies.PerfilOng));
    options.AddPolicy(AuthPolicies.EmpresaOuOng, policy => policy.RequireRole(AuthPolicies.PerfilParceiro, AuthPolicies.PerfilOng));
    options.AddPolicy(AuthPolicies.Administrador, policy => policy.RequireRole(AuthPolicies.PerfilAdministrador));
});

builder.Services.AddHttpClient("FirebaseAuth", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEstabelecimentoRepository, EstabelecimentoRepository>();
builder.Services.AddScoped<IEstabelecimentoService, EstabelecimentoService>();
builder.Services.AddScoped<ISolicitacaoRepository, SolicitacaoRepository>();
builder.Services.AddScoped<ISolicitacaoService, SolicitacaoService>();
builder.Services.AddScoped<ITransacaoFirestore, TransacaoFirestore>();
builder.Services.AddScoped<IImpactoService, ImpactoService>();
builder.Services.AddScoped<IConquistaService, ConquistaService>();
builder.Services.AddScoped<ILogService, LogRepository>();

builder.Services.AddScoped<IFirebaseTokenValidator, FirebaseAdminTokenValidator>();
builder.Services.AddScoped<IAuthGateway, AuthGateway>();
builder.Services.AddScoped<IAutenticacaoService, AutenticacaoService>();

var firebaseSection = builder.Configuration.GetSection("Firebase");
var firebaseEnabled = firebaseSection.GetValue<bool>("Enabled");

if (firebaseEnabled)
{
    var firebaseConfig = new FirebaseConfig();
    firebaseSection.Bind(firebaseConfig);

    if (string.IsNullOrWhiteSpace(firebaseConfig.ProjectId))
        throw new InvalidOperationException(
            "Firebase.ProjectId e obrigatorio quando Firebase.Enabled = true.");

    if (firebaseConfig.UseEmulator)
    {
        Environment.SetEnvironmentVariable(
            "FIRESTORE_EMULATOR_HOST", firebaseConfig.EmulatorHost);
        Environment.SetEnvironmentVariable(
            "FIREBASE_AUTH_EMULATOR_HOST", firebaseConfig.AuthEmulatorHost);
    }

    builder.Services.AddSingleton(firebaseConfig);

    builder.Services.AddSingleton(
        firebaseConfig.UseEmulator
            ? new FirestoreDbBuilder
            {
                ProjectId = firebaseConfig.ProjectId,
                EmulatorDetection = EmulatorDetection.EmulatorOnly
            }.Build()
            : FirestoreDb.Create(firebaseConfig.ProjectId));

    try
    {
        FirebaseApp.Create(new AppOptions { ProjectId = firebaseConfig.ProjectId });
    }
    catch (Exception ex)
    {
        using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var logger = loggerFactory.CreateLogger("EcoByte.Startup");
        logger.LogWarning(
            "Firebase Admin SDK nao foi inicializado. Validacao de ID tokens ficara indisponivel. Motivo: {Motivo}",
            ex.Message);
    }
}
else
{
    builder.Services.AddSingleton(new FirebaseConfig { Enabled = false });
    builder.Services.AddSingleton<FirestoreDb>(_ => null!);
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();