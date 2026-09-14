namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models.Auth;

public interface IAuthGateway
{
    Task<ResultadoCredenciais> AutenticarComSenhaAsync(string email, string senha, CancellationToken cancellationToken = default);

    Task<ResultadoCredenciais> RegistrarComSenhaAsync(string email, string senha, CancellationToken cancellationToken = default);
}