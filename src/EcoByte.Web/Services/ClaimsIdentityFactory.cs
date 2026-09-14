namespace EcoByte.Web.Services;

using System.Security.Claims;
using EcoByte.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;

public static class ClaimsIdentityFactory
{
    public static ClaimsIdentity Criar(
        string uid, string nome, string email, string perfil, string? estabelecimentoId = null)
    {
        var claims = new List<Claim>
        {
            new(AuthClaimTypes.Uid, uid),
            new(AuthClaimTypes.Nome, nome),
            new(AuthClaimTypes.Email, email),
            new(AuthClaimTypes.Perfil, perfil)
        };

        if (!string.IsNullOrWhiteSpace(estabelecimentoId))
            claims.Add(new Claim(AuthClaimTypes.EstabelecimentoId, estabelecimentoId));

        return new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}