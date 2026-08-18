namespace EcoByte.Web.Controllers;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class ConsumidorController : Controller
{
    private readonly ISolicitacaoService _solicitacaoService;
    private readonly IConquistaService _conquistaService;
    private readonly IImpactoService _impactoService;

    public ConsumidorController(
        ISolicitacaoService solicitacaoService,
        IConquistaService conquistaService,
        IImpactoService impactoService)
    {
        _solicitacaoService = solicitacaoService;
        _conquistaService = conquistaService;
        _impactoService = impactoService;
    }

    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        try
        {
            var solicitacoes = await _solicitacaoService.ObterPorConsumidorAsync(uid, 1, 10);
            var total = await _solicitacaoService.ContarPorConsumidorAsync(uid);
            var conquistas = await _conquistaService.ObterConquistasUsuarioAsync(uid);

            ViewBag.Solicitacoes = solicitacoes;
            ViewBag.TotalSolicitacoes = total;
            ViewBag.Conquistas = conquistas;

            return View();
        }
        catch (FirestoreNotConfiguredException)
        {
            return View("ErroFirestore");
        }
    }

    public async Task<IActionResult> MinhasSolicitacoes(int pagina = 1)
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var solicitacoes = await _solicitacaoService.ObterPorConsumidorAsync(uid, pagina, 10);
        var total = await _solicitacaoService.ContarPorConsumidorAsync(uid);

        ViewBag.TotalItens = total;
        ViewBag.Pagina = pagina;
        ViewBag.TamanhoPagina = 10;

        return View(solicitacoes);
    }
}
