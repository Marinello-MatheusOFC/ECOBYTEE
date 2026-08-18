namespace EcoByte.Web.Controllers;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class SolicitacoesController : Controller
{
    private readonly ISolicitacaoService _solicitacaoService;

    public SolicitacoesController(ISolicitacaoService solicitacaoService)
    {
        _solicitacaoService = solicitacaoService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(string produtoId, int quantidade)
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        try
        {
            await _solicitacaoService.CriarSolicitacaoAsync(produtoId, uid, quantidade);
            TempData["Sucesso"] = "Solicitacao criada com sucesso!";
        }
        catch (FirestoreNotConfiguredException)
        {
            TempData["Erro"] = "Servico indisponivel.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction("Detalhes", "Produtos", new { id = produtoId });
    }

    public async Task<IActionResult> Detalhes(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var solicitacao = await _solicitacaoService.ObterPorIdAsync(id);
        if (solicitacao is null) return NotFound();

        return View(new SolicitacaoViewModel
        {
            Id = solicitacao.Id ?? string.Empty,
            Quantidade = solicitacao.Quantidade,
            CriadoEm = solicitacao.CriadoEm.ToDateTime()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Cancelar(string id)
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        try
        {
            await _solicitacaoService.CancelarAsync(id, uid);
            TempData["Sucesso"] = "Solicitacao cancelada.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction("MinhasSolicitacoes", "Consumidor");
    }
}
