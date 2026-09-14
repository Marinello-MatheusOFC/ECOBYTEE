namespace EcoByte.Web.Controllers;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Security;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = AuthPolicies.Autenticado)]
public class SolicitacoesController : Controller
{
    private readonly ISolicitacaoService _solicitacaoService;
    private readonly IEstabelecimentoService _estabelecimentoService;

    public SolicitacoesController(
        ISolicitacaoService solicitacaoService,
        IEstabelecimentoService estabelecimentoService)
    {
        _solicitacaoService = solicitacaoService;
        _estabelecimentoService = estabelecimentoService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.Consumidor)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(string produtoId, int quantidade)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
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

        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var solicitacao = await _solicitacaoService.ObterPorIdAsync(id);
        if (solicitacao is null) return NotFound();

        if (!await PermiteVisualizarAsync(solicitacao, uid))
            return Forbid();

        return View(new SolicitacaoViewModel
        {
            Id = solicitacao.Id ?? string.Empty,
            Quantidade = solicitacao.Quantidade,
            CriadoEm = solicitacao.CriadoEm.ToDateTime()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(string id)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
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

    private async Task<bool> PermiteVisualizarAsync(
        EcoByte.Web.Models.Solicitacao solicitacao, string uid)
    {
        if (solicitacao.ConsumidorId == uid)
            return true;

        if (string.IsNullOrWhiteSpace(solicitacao.EstabelecimentoId))
            return false;

        var estabelecimento = await _estabelecimentoService
            .ObterPorIdAsync(solicitacao.EstabelecimentoId);

        return estabelecimento?.UsuarioResponsavelId == uid;
    }
}