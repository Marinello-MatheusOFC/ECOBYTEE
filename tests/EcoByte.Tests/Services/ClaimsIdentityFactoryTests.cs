using System.Security.Claims;
using EcoByte.Web.Security;
using EcoByte.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EcoByte.Tests.Services;

public class ClaimsIdentityFactoryTests
{
    [Fact]
    public void Criar_SemEstabelecimento_GeraClaimsBase()
    {
        var identity = ClaimsIdentityFactory.Criar("uid1", "Ana", "ana@exemplo.com", "Consumidor");

        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, identity.AuthenticationType);
        Assert.Equal("uid1", identity.FindFirst(AuthClaimTypes.Uid)!.Value);
        Assert.Equal("Ana", identity.FindFirst(AuthClaimTypes.Nome)!.Value);
        Assert.Equal("ana@exemplo.com", identity.FindFirst(AuthClaimTypes.Email)!.Value);
        Assert.Equal("Consumidor", identity.FindFirst(AuthClaimTypes.Perfil)!.Value);
        Assert.Null(identity.FindFirst(AuthClaimTypes.EstabelecimentoId));
    }

    [Fact]
    public void Criar_ComEstabelecimento_IncluiClaimDeEstabelecimento()
    {
        var identity = ClaimsIdentityFactory.Criar("uid1", "Ana", "ana@exemplo.com", "Parceiro", "estab1");

        Assert.Equal("estab1", identity.FindFirst(AuthClaimTypes.EstabelecimentoId)!.Value);
    }

    [Fact]
    public void Criar_AutorizacaoUtilizaPerfilComoRole()
    {
        var identity = ClaimsIdentityFactory.Criar("uid1", "Ana", "ana@exemplo.com", "Administrador");

        Assert.True(identity.HasClaim(ClaimTypes.Role, "Administrador"));
        Assert.Equal("uid1", identity.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}