namespace EcoByte.Web.Areas.Empresa.Controllers;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Security;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Empresa")]
[Authorize(Policy = AuthPolicies.Parceiro)]
public class EstabelecimentosController : Controller
{
    private readonly IEstabelecimentoService _estabelecimentoService;

    public EstabelecimentosController(IEstabelecimentoService estabelecimentoService)
    {
        _estabelecimentoService = estabelecimentoService;
    }

    public async Task<IActionResult> Index()
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var estabelecimento = await _estabelecimentoService.ObterPorUsuarioAsync(uid);

        if (estabelecimento is null)
            return RedirectToAction(nameof(Criar));

        return View(estabelecimento);
    }

    public IActionResult Criar()
    {
        return View(new EstabelecimentoViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(EstabelecimentoViewModel model)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        if (!ModelState.IsValid) return View(model);

        try
        {
            await _estabelecimentoService.CriarAsync(
                uid, model.NomeFantasia, model.Telefone, model.Email,
                model.Descricao, model.Endereco);

            TempData["Sucesso"] = "Estabelecimento cadastrado e enviado para aprovacao.";
            return RedirectToAction("Index", "Home", new { area = "Empresa" });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Editar()
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        var estabelecimento = await _estabelecimentoService.ObterPorUsuarioAsync(uid);
        if (estabelecimento is null) return RedirectToAction(nameof(Criar));

        return View(new EstabelecimentoViewModel
        {
            Id = estabelecimento.Id,
            NomeFantasia = estabelecimento.NomeFantasia,
            RazaoSocial = estabelecimento.RazaoSocial,
            Telefone = estabelecimento.Telefone,
            Email = estabelecimento.Email,
            Descricao = estabelecimento.Descricao,
            Endereco = estabelecimento.Endereco
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EstabelecimentoViewModel model)
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();

        if (!ModelState.IsValid) return View(model);

        var estabelecimento = await _estabelecimentoService.ObterPorUsuarioAsync(uid);
        if (estabelecimento is null || model.Id != estabelecimento.Id)
            return Forbid();

        await _estabelecimentoService.AtualizarAsync(
            model.Id!, model.NomeFantasia, model.Telefone, model.Email,
            model.Descricao, model.Endereco);

        TempData["Sucesso"] = "Estabelecimento atualizado.";
        return RedirectToAction(nameof(Index));
    }
}