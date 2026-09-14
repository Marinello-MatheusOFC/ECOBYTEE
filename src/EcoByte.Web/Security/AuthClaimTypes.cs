namespace EcoByte.Web.Security;

using System.Security.Claims;

public static class AuthClaimTypes
{
    public const string Uid = ClaimTypes.NameIdentifier;
    public const string Nome = ClaimTypes.Name;
    public const string Email = ClaimTypes.Email;
    public const string Perfil = ClaimTypes.Role;
    public const string EstabelecimentoId = "estabelecimento_id";
}