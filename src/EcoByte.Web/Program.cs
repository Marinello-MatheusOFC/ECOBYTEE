using EcoByte.Web.Firebase;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Repositories;
using EcoByte.Web.Services;
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
        options.AccessDeniedPath = "/Shared/Error/403";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEstabelecimentoRepository, EstabelecimentoRepository>();
builder.Services.AddScoped<IEstabelecimentoService, EstabelecimentoService>();
builder.Services.AddScoped<ISolicitacaoRepository, SolicitacaoRepository>();
builder.Services.AddScoped<ISolicitacaoService, SolicitacaoService>();
builder.Services.AddScoped<IImpactoService, ImpactoService>();
builder.Services.AddScoped<IConquistaService, ConquistaService>();
builder.Services.AddScoped<ILogService, LogRepository>();

var firebaseSection = builder.Configuration.GetSection("Firebase");
var firebaseEnabled = firebaseSection.GetValue<bool>("Enabled");

if (firebaseEnabled)
{
    var firebaseConfig = new FirebaseConfig();
    firebaseSection.Bind(firebaseConfig);

    if (firebaseConfig.UseEmulator)
    {
        Environment.SetEnvironmentVariable(
            "FIRESTORE_EMULATOR_HOST", firebaseConfig.EmulatorHost);
    }

    var firestoreDb = FirestoreDb.Create(firebaseConfig.ProjectId);
    builder.Services.AddSingleton(firestoreDb);
    builder.Services.AddSingleton(firebaseConfig);
}
else
{
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
