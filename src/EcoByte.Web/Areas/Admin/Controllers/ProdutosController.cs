namespace EcoByte.Web.Areas.Admin.Controllers;

using EcoByte.Web.Enums;
using EcoByte.Web.Exceptions;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.Services;
using EcoByte.Web.ViewModels;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[Authorize(Roles = "Administrador")]
public class ProdutosController : Controller
{
    private readonly IProdutoService _produtoService;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IEstabelecimentoService _estabelecimentoService;

    public ProdutosController(
        IProdutoService produtoService,
        IProdutoRepository produtoRepository,
        IEstabelecimentoService estabelecimentoService)
    {
        _produtoService = produtoService;
        _produtoRepository = produtoRepository;
        _estabelecimentoService = estabelecimentoService;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var filtro = new ProdutoFiltroViewModel { Pagina = pagina, TamanhoPagina = 20 };
        filtro.TotalItens = await _produtoService.ContarProdutosDisponiveisAsync(filtro);
        var produtos = await _produtoService.ObterProdutosDisponiveisAsync(filtro);

        ViewBag.Pagina = pagina;
        ViewBag.TotalItens = filtro.TotalItens;
        return View(produtos);
    }

    public async Task<IActionResult> Details(string id)
    {
        var produto = await _produtoService.ObterDetalhesAsync(id);
        if (produto is null) return NotFound();
        return View(produto);
    }

    public IActionResult Create()
    {
        return View(new ProdutoFormularioViewModel { DataLimite = DateTime.Now.AddDays(7) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProdutoFormularioViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var produto = new Models.Produto
            {
                Nome = model.Nome,
                Descricao = model.Descricao,
                PrecoOriginalCentavos = ProdutoService.DecimalParaCentavos(model.PrecoOriginal),
                PrecoPromocionalCentavos = ProdutoService.DecimalParaCentavos(model.PrecoPromocional),
                QuantidadeDisponivel = model.QuantidadeDisponivel,
                DataLimite = Timestamp.FromDateTime(model.DataLimite),
                ImagemUrl = model.ImagemUrl,
                Categoria = model.Categoria.ToFirestoreString(),
                Status = StatusProduto.Rascunho.ToFirestoreString(),
                EhVegano = model.EhVegano,
                EhSemGluten = model.EhSemGluten,
                EhSemLactose = model.EhSemLactose,
                EstabelecimentoId = string.Empty,
                CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            TempData["Sucesso"] = "Produto criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Erro ao criar produto.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(string id)
    {
        var produto = await _produtoService.ObterDetalhesAsync(id);
        if (produto is null) return NotFound();

        var model = new ProdutoFormularioViewModel
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            PrecoOriginal = produto.PrecoOriginal,
            PrecoPromocional = produto.PrecoPromocional,
            QuantidadeDisponivel = produto.QuantidadeDisponivel,
            DataLimite = produto.DataLimite,
            ImagemUrl = produto.ImagemUrl,
            Categoria = produto.Categoria,
            EhVegano = produto.EhVegano,
            EhSemGluten = produto.EhSemGluten,
            EhSemLactose = produto.EhSemLactose
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, ProdutoFormularioViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        TempData["Sucesso"] = "Produto atualizado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var produto = await _produtoService.ObterDetalhesAsync(id);
        if (produto is null) return NotFound();
        return View(produto);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        TempData["Sucesso"] = "Produto desativado.";
        return RedirectToAction(nameof(Index));
    }
}
