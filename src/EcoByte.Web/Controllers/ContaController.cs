namespace EcoByte.Web.Controllers;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Security;
using EcoByte.Web.Services;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public class ContaController : Controller
{
    private readonly IAutenticacaoService _autenticacaoService;
    private readonly ILogService _logService;

    public ContaController(IAutenticacaoService autenticacaoService, ILogService logService)
    {
        _autenticacaoService = autenticacaoService;
        _logService = logService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var resultado = await _autenticacaoService.LoginAsync(
            model.Email, model.Senha, cancellationToken);

        if (!resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.Uid))
        {
            ModelState.AddModelError(string.Empty, resultado.Erro ?? "Credenciais invalidas.");
            return View(model);
        }

        var perfil = resultado.Perfil ?? string.Empty;
        var nome = string.IsNullOrWhiteSpace(resultado.Nome)
            ? (resultado.Email ?? "Usuario")
            : resultado.Nome!;

        var identity = ClaimsIdentityFactory.Criar(
            resultado.Uid, nome, resultado.Email ?? string.Empty, perfil, resultado.EstabelecimentoId);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _logService.RegistrarAsync(resultado.Uid, "Login", "Usuario", resultado.Uid, "Sucesso");

        if (!resultado.PossuiPerfil)
            return RedirectToAction("Index", "Onboarding");

        if (perfil == AuthPolicies.PerfilAdministrador)
            return RedirectToAction("Index", "Home", new { area = "Admin" });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegistroViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(
        RegistroViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var resultado = await _autenticacaoService.RegistrarAsync(
            model.Nome, model.Email, model.Senha, cancellationToken);

        if (!resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.Uid))
        {
            ModelState.AddModelError(string.Empty, resultado.Erro ?? "Erro ao criar conta.");
            return View(model);
        }

        var identity = ClaimsIdentityFactory.Criar(
            resultado.Uid,
            model.Nome,
            resultado.Email ?? model.Email,
            resultado.Perfil ?? AuthPolicies.PerfilConsumidor,
            resultado.EstabelecimentoId);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _logService.RegistrarAsync(resultado.Uid, "Registro", "Usuario", resultado.Uid, "Sucesso");

        return RedirectToAction("Index", "Onboarding");
    }

    [HttpGet]
    public IActionResult AcessoNegado()
    {
        return View("Error/403");
    }

    public async Task<IActionResult> Logout()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!string.IsNullOrWhiteSpace(uid))
            await _logService.RegistrarAsync(uid, "Logout", "Usuario", uid, "Sucesso");

        return RedirectToAction("Index", "Home");
    }
}