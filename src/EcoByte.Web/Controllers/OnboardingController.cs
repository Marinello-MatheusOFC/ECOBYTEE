namespace EcoByte.Web.Controllers;

using EcoByte.Web.Enums;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Security;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class OnboardingController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly ILogService _logService;

    public OnboardingController(IUsuarioService usuarioService, ILogService logService)
    {
        _usuarioService = usuarioService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var usuario = await _usuarioService.ObterPorUidAsync(uid);

        var viewModel = new OnboardingViewModel
        {
            Nome = usuario?.Nome ?? User.Identity?.Name ?? string.Empty,
            PerfilSelecionadoAtual = usuario?.Perfil ?? string.Empty
        };

        ViewBag.ReturnUrl = returnUrl;
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(OnboardingViewModel model)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        if (!ModelState.IsValid) return View(model);

        if (model.Perfil is null)
        {
            ModelState.AddModelError(string.Empty, "Selecione um perfil.");
            return View(model);
        }

        var perfil = model.Perfil.Value.ToString();

        if (perfil == AuthPolicies.PerfilAdministrador)
        {
            ModelState.AddModelError(string.Empty, "Perfil nao permitido.");
            return View(model);
        }

        var email = User.FindFirst(AuthClaimTypes.Email)?.Value ?? string.Empty;

        await _usuarioService.DefinirPerfilAsync(uid, email, model.Nome, perfil);
        await _logService.RegistrarAsync(uid, "Onboarding", "Usuario", uid, "Sucesso");

        if (perfil == AuthPolicies.PerfilParceiro)
            return RedirectToAction("Criar", "Estabelecimentos", new { area = "Empresa" });

        return RedirectToAction("Index", "Home");
    }
}