namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models.Auth;

public interface IFirebaseTokenValidator
{
    Task<ValidacaoTokenResultado> ValidarAsync(string? idToken, CancellationToken cancellationToken = default);
}