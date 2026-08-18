namespace EcoByte.Web.Controllers;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

public class ProdutosController : Controller
{
    private readonly IProdutoService _produtoService;

    public ProdutosController(IProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    public async Task<IActionResult> Index(ProdutoFiltroViewModel filtro)
    {
        try
        {
            filtro.TotalItens = await _produtoService
                .ContarProdutosDisponiveisAsync(filtro);

            var produtos = await _produtoService
                .ObterProdutosDisponiveisAsync(filtro);

            ViewBag.Filtro = filtro;
            return View(produtos);
        }
        catch (FirestoreNotConfiguredException)
        {
            ViewBag.ErroFirestore = true;
            ViewBag.Filtro = filtro;
            return View(new List<ProdutoResumoViewModel>());
        }
    }

    public async Task<IActionResult> Detalhes(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        try
        {
            var produto = await _produtoService.ObterDetalhesAsync(id);

            if (produto is null)
                return NotFound();

            return View(produto);
        }
        catch (FirestoreNotConfiguredException)
        {
            return RedirectToAction("Index");
        }
    }
}
