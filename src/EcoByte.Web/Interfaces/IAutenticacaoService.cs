namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models.Auth;

public interface IAutenticacaoService
{
    Task<ResultadoAutenticacao> LoginAsync(string email, string senha, CancellationToken cancellationToken = default);

    Task<ResultadoAutenticacao> RegistrarAsync(string nome, string email, string senha, CancellationToken cancellationToken = default);
}