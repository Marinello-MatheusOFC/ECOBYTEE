namespace EcoByte.Web.Controllers;

using EcoByte.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class RetiradasController : Controller
{
    private readonly ISolicitacaoService _solicitacaoService;

    public RetiradasController(ISolicitacaoService solicitacaoService)
    {
        _solicitacaoService = solicitacaoService;
    }

    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var solicitacoes = await _solicitacaoService.ObterTodasAsync(1, 50);
        return View(solicitacoes);
    }

    [HttpPost]
    public async Task<IActionResult> Confirmar(string id)
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        try
        {
            await _solicitacaoService.ConfirmarRetiradaAsync(id, uid);
            TempData["Sucesso"] = "Retirada confirmada.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
