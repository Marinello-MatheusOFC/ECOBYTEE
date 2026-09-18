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

    public async Task<IActionResult> Create()
    {
        await CarregarEstabelecimentosAsync();
        return View(new ProdutoFormularioViewModel { DataLimite = DateTime.Now.AddDays(7) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProdutoFormularioViewModel model)
    {
        if (!ModelState.IsValid || await EstabelecimentoInvalidoAsync(model))
        {
            await CarregarEstabelecimentosAsync();
            return View(model);
        }

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
                Status = StatusProduto.Disponivel.ToFirestoreString(),
                EhVegano = model.EhVegano,
                EhSemGluten = model.EhSemGluten,
                EhSemLactose = model.EhSemLactose,
                EstabelecimentoId = model.EstabelecimentoId!,
                CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            await _produtoRepository.CriarAsync(produto);

            TempData["Sucesso"] = "Produto criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await CarregarEstabelecimentosAsync();
            ModelState.AddModelError(string.Empty, "Erro ao criar produto.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(string id)
    {
        var produto = await ObterProdutoOuNulo(id);
        if (produto is null) return NotFound();

        var model = new ProdutoFormularioViewModel
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            PrecoOriginal = ProdutoService.CentavosParaDecimal(produto.PrecoOriginalCentavos),
            PrecoPromocional = ProdutoService.CentavosParaDecimal(produto.PrecoPromocionalCentavos),
            QuantidadeDisponivel = produto.QuantidadeDisponivel,
            DataLimite = produto.DataLimite?.ToDateTime() ?? DateTime.Now,
            ImagemUrl = produto.ImagemUrl,
            Categoria = EnumFirestoreExtensions.ParseCategoria(produto.Categoria) ?? CategoriaProduto.Outros,
            EhVegano = produto.EhVegano,
            EhSemGluten = produto.EhSemGluten,
            EhSemLactose = produto.EhSemLactose,
            EstabelecimentoId = produto.EstabelecimentoId
        };

        await CarregarEstabelecimentosAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ProdutoFormularioViewModel model)
    {
        if (!ModelState.IsValid || await EstabelecimentoInvalidoAsync(model))
        {
            await CarregarEstabelecimentosAsync();
            return View(model);
        }

        var produto = await ObterProdutoOuNulo(id);
        if (produto is null) return NotFound();

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
        produto.EstabelecimentoId = model.EstabelecimentoId!;

        await _produtoRepository.AtualizarAsync(produto);

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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var produto = await ObterProdutoOuNulo(id);
        if (produto is not null)
        {
            produto.Status = StatusProduto.Desativado.ToFirestoreString();
            await _produtoRepository.AtualizarAsync(produto);
        }

        TempData["Sucesso"] = "Produto desativado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Models.Produto?> ObterProdutoOuNulo(string id)
    {
        try
        {
            return await _produtoRepository.ObterPorIdAsync(id);
        }
        catch (FirestoreNotConfiguredException)
        {
            return null;
        }
    }

    private async Task<bool> EstabelecimentoInvalidoAsync(ProdutoFormularioViewModel model)
    {
        var estabelecimento = await _estabelecimentoService.ObterPorIdAsync(model.EstabelecimentoId ?? string.Empty);
        if (estabelecimento is not null) return false;

        ModelState.AddModelError(nameof(model.EstabelecimentoId), "Estabelecimento invalido.");
        return true;
    }

    private async Task CarregarEstabelecimentosAsync()
    {
        var estabelecimentos = await _estabelecimentoService.ObterTodosAsync(1, 500);
        ViewBag.Estabelecimentos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            estabelecimentos, nameof(Estabelecimento.Id), nameof(Estabelecimento.NomeFantasia));
    }
}