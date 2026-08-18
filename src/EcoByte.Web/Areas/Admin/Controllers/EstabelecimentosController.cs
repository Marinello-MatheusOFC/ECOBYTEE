namespace EcoByte.Web.Areas.Admin.Controllers;

using EcoByte.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[Authorize(Roles = "Administrador")]
public class EstabelecimentosController : Controller
{
    private readonly IEstabelecimentoService _estabelecimentoService;

    public EstabelecimentosController(IEstabelecimentoService estabelecimentoService)
    {
        _estabelecimentoService = estabelecimentoService;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var estabelecimentos = await _estabelecimentoService.ObterTodosAsync(pagina, 20);
        var total = await _estabelecimentoService.ContarTodosAsync();

        ViewBag.Pagina = pagina;
        ViewBag.TotalItens = total;
        ViewBag.TamanhoPagina = 20;

        return View(estabelecimentos);
    }
}
