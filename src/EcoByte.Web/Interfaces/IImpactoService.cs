namespace EcoByte.Web.Interfaces;

using EcoByte.Web.ViewModels;

public interface IImpactoService
{
    Task<ImpactoResumoViewModel> ObterImpactoAsync(string? usuarioUid);
}
