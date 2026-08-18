namespace EcoByte.Web.Controllers;

using EcoByte.Web.Interfaces;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class PerfilController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public PerfilController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var usuario = await _usuarioService.ObterPorUidAsync(uid);
        if (usuario is null) return NotFound();

        var viewModel = new PerfilViewModel
        {
            Uid = usuario.Uid,
            Nome = usuario.Nome,
            Email = usuario.Email,
            CriadoEm = usuario.CriadoEm.ToDateTime()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Editar()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var usuario = await _usuarioService.ObterPorUidAsync(uid);
        if (usuario is null) return NotFound();

        return View(new PerfilEditarViewModel { Nome = usuario.Nome });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(PerfilEditarViewModel model)
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        if (!ModelState.IsValid) return View(model);

        await _usuarioService.AtualizarPerfilAsync(uid, model.Nome);
        return RedirectToAction(nameof(Index));
    }
}
