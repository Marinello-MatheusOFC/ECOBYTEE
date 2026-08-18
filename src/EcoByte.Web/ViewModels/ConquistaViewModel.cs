namespace EcoByte.Web.ViewModels;

public class ConquistaViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public bool Desbloqueada { get; set; }
    public DateTime? DesbloqueadaEm { get; set; }
}
