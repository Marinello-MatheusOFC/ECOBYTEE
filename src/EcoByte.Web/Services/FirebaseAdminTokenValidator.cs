namespace EcoByte.Web.Services;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Models.Auth;
using FirebaseAdmin.Auth;

public class FirebaseAdminTokenValidator : IFirebaseTokenValidator
{
    public async Task<ValidacaoTokenResultado> ValidarAsync(
        string? idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return ValidacaoTokenResultado.Falha("Token ausente.");

        try
        {
            var firebaseToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken, cancellationToken);

            if (string.IsNullOrWhiteSpace(firebaseToken.Uid))
                return ValidacaoTokenResultado.Falha("Token invalido.");

            var email = firebaseToken.Claims.TryGetValue("email", out var claimEmail)
                ? claimEmail as string
                : null;

            var emailVerificado = firebaseToken.Claims.TryGetValue("email_verified", out var claimVerificado)
                && claimVerificado is bool verificada
                && verificada;

            return ValidacaoTokenResultado.Sucesso(firebaseToken.Uid, email, emailVerificado);
        }
        catch (FirebaseAuthException)
        {
            return ValidacaoTokenResultado.Falha("Token invalido ou expirado.");
        }
        catch (InvalidOperationException)
        {
            return ValidacaoTokenResultado.Falha("Servico de autenticacao indisponivel.");
        }
        catch (Exception)
        {
            return ValidacaoTokenResultado.Falha("Nao foi possivel validar o token.");
        }
    }
}