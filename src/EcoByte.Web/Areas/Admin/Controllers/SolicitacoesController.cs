namespace EcoByte.Web.Areas.Admin.Controllers;

using EcoByte.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[Authorize(Roles = "Administrador")]
public class SolicitacoesController : Controller
{
    private readonly ISolicitacaoService _solicitacaoService;

    public SolicitacoesController(ISolicitacaoService solicitacaoService)
    {
        _solicitacaoService = solicitacaoService;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var solicitacoes = await _solicitacaoService.ObterTodasAsync(pagina, 20);
        var total = await _solicitacaoService.ContarTodasAsync();

        ViewBag.Pagina = pagina;
        ViewBag.TotalItens = total;
        ViewBag.TamanhoPagina = 20;

        return View(solicitacoes);
    }
}
