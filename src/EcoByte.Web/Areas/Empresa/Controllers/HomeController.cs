namespace EcoByte.Web.Areas.Empresa.Controllers;

using EcoByte.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Empresa")]
[Authorize(Policy = AuthPolicies.Parceiro)]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}