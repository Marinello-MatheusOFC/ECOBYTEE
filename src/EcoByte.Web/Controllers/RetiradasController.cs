namespace EcoByte.Web.Controllers;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = AuthPolicies.Parceiro)]
public class RetiradasController : Controller
{
    private readonly ISolicitacaoService _solicitacaoService;
    private readonly IEstabelecimentoService _estabelecimentoService;

    public RetiradasController(
        ISolicitacaoService solicitacaoService,
        IEstabelecimentoService estabelecimentoService)
    {
        _solicitacaoService = solicitacaoService;
        _estabelecimentoService = estabelecimentoService;
    }

    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var estabelecimento = await _estabelecimentoService.ObterPorUsuarioAsync(uid);
        if (estabelecimento is null)
            return RedirectToAction("Criar", "Estabelecimentos", new { area = "Empresa" });

        var solicitacoes = await _solicitacaoService
            .ObterPorEstabelecimentoAsync(estabelecimento.Id!, 1, 50);

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