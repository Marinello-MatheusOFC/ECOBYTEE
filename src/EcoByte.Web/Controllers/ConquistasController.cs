namespace EcoByte.Web.Controllers;

using EcoByte.Web.Interfaces;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class ConquistasController : Controller
{
    private readonly IConquistaService _conquistaService;

    public ConquistasController(IConquistaService conquistaService)
    {
        _conquistaService = conquistaService;
    }

    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        await _conquistaService.VerificarEConcederAsync(uid);

        var todas = await _conquistaService.ObterTodasAsync();
        var doUsuario = await _conquistaService.ObterConquistasUsuarioAsync(uid);
        var idsDoUsuario = doUsuario.Select(c => c.Id).ToHashSet();

        var viewModels = todas.Select(c => new ConquistaViewModel
        {
            Id = c.Id ?? string.Empty,
            Nome = c.Nome,
            Descricao = c.Descricao,
            Icone = c.Icone,
            Desbloqueada = c.Id is not null && idsDoUsuario.Contains(c.Id)
        }).ToList();

        return View(viewModels);
    }
}
