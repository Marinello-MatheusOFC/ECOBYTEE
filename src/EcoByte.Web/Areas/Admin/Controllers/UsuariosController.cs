namespace EcoByte.Web.Areas.Admin.Controllers;

using EcoByte.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var usuarios = await _usuarioService.ObterTodosAsync(pagina, 20);
        var total = await _usuarioService.ContarTodosAsync();

        ViewBag.Pagina = pagina;
        ViewBag.TotalItens = total;
        ViewBag.TamanhoPagina = 20;

        return View(usuarios);
    }
}
