namespace EcoByte.Web.Areas.Empresa.Controllers;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Empresa")]
[Authorize(Policy = AuthPolicies.Parceiro)]
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

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrWhiteSpace(uid)) return Unauthorized();

        var estabelecimento = await _estabelecimentoService.ObterPorUsuarioAsync(uid);
        if (estabelecimento is null)
            return RedirectToAction("Criar", "Estabelecimentos", new { area = "Empresa" });

        var solicitacoes = await _solicitacaoService
            .ObterPorEstabelecimentoAsync(estabelecimento.Id!, pagina, 20);
        var total = await _solicitacaoService
            .ContarPorEstabelecimentoAsync(estabelecimento.Id!);

        ViewBag.TotalItens = total;
        ViewBag.Pagina = pagina;
        ViewBag.TamanhoPagina = 20;

        return View(solicitacoes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(string id)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
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