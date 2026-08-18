namespace EcoByte.Web.Areas.Admin.Controllers;

using EcoByte.Web.Interfaces;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[Authorize(Roles = "Administrador")]
public class HomeController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly IEstabelecimentoService _estabelecimentoService;
    private readonly ISolicitacaoService _solicitacaoService;
    private readonly IProdutoRepository _produtoRepository;

    public HomeController(
        IUsuarioService usuarioService,
        IEstabelecimentoService estabelecimentoService,
        ISolicitacaoService solicitacaoService,
        IProdutoRepository produtoRepository)
    {
        _usuarioService = usuarioService;
        _estabelecimentoService = estabelecimentoService;
        _solicitacaoService = solicitacaoService;
        _produtoRepository = produtoRepository;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new DashboardViewModel
        {
            TotalUsuarios = await _usuarioService.ContarTodosAsync(),
            TotalEstabelecimentos = await _estabelecimentoService.ContarTodosAsync(),
            TotalSolicitacoes = await _solicitacaoService.ContarTodasAsync()
        };

        return View(viewModel);
    }
}
