namespace EcoByte.Web.Areas.Empresa.Controllers;

using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.Security;
using EcoByte.Web.Services;
using EcoByte.Web.ViewModels;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Empresa")]
[Authorize(Policy = AuthPolicies.Parceiro)]
public class ProdutosController : Controller
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IEstabelecimentoService _estabelecimentoService;

    public ProdutosController(
        IProdutoRepository produtoRepository,
        IEstabelecimentoService estabelecimentoService)
    {
        _produtoRepository = produtoRepository;
        _estabelecimentoService = estabelecimentoService;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var estabelecimentoId = await ObterEstabelecimentoIdAsync();
        if (string.IsNullOrWhiteSpace(estabelecimentoId))
            return RedirectToAction("Criar", "Estabelecimentos", new { area = "Empresa" });

        var produtos = await _produtoRepository
            .ObterPorEstabelecimentoAsync(estabelecimentoId, 50, (pagina - 1) * 50);
        var total = await _produtoRepository.ContarPorEstabelecimentoAsync(estabelecimentoId);

        ViewBag.TotalItens = total;
        ViewBag.Pagina = pagina;
        ViewBag.TamanhoPagina = 50;

        return View(produtos);
    }

    public IActionResult Criar()
    {
        return View(new ProdutoFormularioViewModel { DataLimite = DateTime.Now.AddDays(7) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(ProdutoFormularioViewModel model)
    {
        var estabelecimentoId = await ObterEstabelecimentoIdAsync();
        if (string.IsNullOrWhiteSpace(estabelecimentoId))
            return RedirectToAction("Criar", "Estabelecimentos", new { area = "Empresa" });

        if (!ModelState.IsValid) return View(model);

        var produto = new Produto
        {
            Nome = model.Nome,
            Descricao = model.Descricao,
            PrecoOriginalCentavos = ProdutoService.DecimalParaCentavos(model.PrecoOriginal),
            PrecoPromocionalCentavos = ProdutoService.DecimalParaCentavos(model.PrecoPromocional),
            QuantidadeDisponivel = model.QuantidadeDisponivel,
            DataLimite = Timestamp.FromDateTime(model.DataLimite),
            ImagemUrl = model.ImagemUrl,
            Categoria = model.Categoria.ToFirestoreString(),
            Status = Enums.StatusProduto.Rascunho.ToFirestoreString(),
            EhVegano = model.EhVegano,
            EhSemGluten = model.EhSemGluten,
            EhSemLactose = model.EhSemLactose,
            EstabelecimentoId = estabelecimentoId,
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        await _produtoRepository.CriarAsync(produto);

        TempData["Sucesso"] = "Produto criado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(string id)
    {
        var estabelecimentoId = await ObterEstabelecimentoIdAsync();
        if (string.IsNullOrWhiteSpace(estabelecimentoId))
            return RedirectToAction("Criar", "Estabelecimentos", new { area = "Empresa" });

        var produto = await _produtoRepository.ObterPorIdAsync(id);
        if (produto is null) return NotFound();

        if (produto.EstabelecimentoId != estabelecimentoId)
            return Forbid();

        return View(new ProdutoFormularioViewModel
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            PrecoOriginal = ProdutoService.CentavosParaDecimal(produto.PrecoOriginalCentavos),
            PrecoPromocional = ProdutoService.CentavosParaDecimal(produto.PrecoPromocionalCentavos),
            QuantidadeDisponivel = produto.QuantidadeDisponivel,
            DataLimite = produto.DataLimite?.ToDateTime() ?? DateTime.Now,
            ImagemUrl = produto.ImagemUrl ?? string.Empty,
            Categoria = EnumFirestoreExtensions.ParseCategoria(produto.Categoria) ?? Enums.CategoriaProduto.Outros,
            EhVegano = produto.EhVegano,
            EhSemGluten = produto.EhSemGluten,
            EhSemLactose = produto.EhSemLactose
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(string id, ProdutoFormularioViewModel model)
    {
        var estabelecimentoId = await ObterEstabelecimentoIdAsync();
        if (string.IsNullOrWhiteSpace(estabelecimentoId))
            return RedirectToAction("Criar", "Estabelecimentos", new { area = "Empresa" });

        if (!ModelState.IsValid) return View(model);

        var produto = await _produtoRepository.ObterPorIdAsync(id);
        if (produto is null) return NotFound();

        if (produto.EstabelecimentoId != estabelecimentoId)
            return Forbid();

        produto.Nome = model.Nome;
        produto.Descricao = model.Descricao;
        produto.PrecoOriginalCentavos = ProdutoService.DecimalParaCentavos(model.PrecoOriginal);
        produto.PrecoPromocionalCentavos = ProdutoService.DecimalParaCentavos(model.PrecoPromocional);
        produto.QuantidadeDisponivel = model.QuantidadeDisponivel;
        produto.DataLimite = Timestamp.FromDateTime(model.DataLimite);
        produto.ImagemUrl = model.ImagemUrl;
        produto.Categoria = model.Categoria.ToFirestoreString();
        produto.EhVegano = model.EhVegano;
        produto.EhSemGluten = model.EhSemGluten;
        produto.EhSemLactose = model.EhSemLactose;

        await _produtoRepository.AtualizarAsync(produto);

        TempData["Sucesso"] = "Produto atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(string id)
    {
        var estabelecimentoId = await ObterEstabelecimentoIdAsync();
        if (string.IsNullOrWhiteSpace(estabelecimentoId))
            return RedirectToAction("Criar", "Estabelecimentos", new { area = "Empresa" });

        var produto = await _produtoRepository.ObterPorIdAsync(id);
        if (produto is null) return NotFound();

        if (produto.EstabelecimentoId != estabelecimentoId)
            return Forbid();

        produto.Status = Enums.StatusProduto.Desativado.ToFirestoreString();
        await _produtoRepository.AtualizarAsync(produto);

        TempData["Sucesso"] = "Produto desativado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> ObterEstabelecimentoIdAsync()
    {
        var uid = User.FindFirst(AuthClaimTypes.Uid)?.Value;
        if (string.IsNullOrWhiteSpace(uid)) return null;

        var estabelecimento = await _estabelecimentoService.ObterPorUsuarioAsync(uid);
        return estabelecimento?.Id;
    }
}