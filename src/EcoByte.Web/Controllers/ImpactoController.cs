namespace EcoByte.Web.Controllers;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class ImpactoController : Controller
{
    private readonly IImpactoService _impactoService;

    public ImpactoController(IImpactoService impactoService)
    {
        _impactoService = impactoService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var impacto = await _impactoService.ObterImpactoAsync(null);
            return View(impacto);
        }
        catch (FirestoreNotConfiguredException)
        {
            return View(new ImpactoResumoViewModel());
        }
    }
}
