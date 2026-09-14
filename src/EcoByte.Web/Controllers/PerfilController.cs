namespace EcoByte.Web.Controllers;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Security;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = AuthPolicies.Autenticado)]
public class PerfilController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public PerfilController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var usuario = await _usuarioService.ObterPorUidAsync(uid);
        if (usuario is null) return RedirectToAction("Index", "Onboarding");

        var viewModel = new PerfilViewModel
        {
            Uid = usuario.Uid,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = Enum.TryParse<Enums.PerfilUsuario>(usuario.Perfil, true, out var perfil)
                ? perfil
                : Enums.PerfilUsuario.Consumidor,
            CriadoEm = usuario.CriadoEm.ToDateTime()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Editar()
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var usuario = await _usuarioService.ObterPorUidAsync(uid);
        if (usuario is null) return RedirectToAction("Index", "Onboarding");

        return View(new PerfilEditarViewModel { Nome = usuario.Nome });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(PerfilEditarViewModel model)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        if (!ModelState.IsValid) return View(model);

        await _usuarioService.AtualizarPerfilAsync(uid, model.Nome);
        return RedirectToAction(nameof(Index));
    }
}
