namespace EcoByte.Web.ViewModels;

using EcoByte.Web.Enums;

public class PerfilViewModel
{
    public string Uid { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public string PerfilDescricao => Perfil.ToString();
    public DateTime CriadoEm { get; set; }
}
