namespace EcoByte.Web.ViewModels;

using EcoByte.Web.Enums;

public class SolicitacaoDetalhesViewModel
{
    public string Id { get; set; } = string.Empty;
    public string ProdutoId { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;
    public string ProdutoImagemUrl { get; set; } = string.Empty;
    public string ConsumidorNome { get; set; } = string.Empty;
    public string EstabelecimentoNome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total => PrecoUnitario * Quantidade;
    public StatusSolicitacao Status { get; set; }
    public string StatusDescricao => Status.ToString();
    public DateTime CriadoEm { get; set; }
    public DateTime? ReservadoAte { get; set; }
    public DateTime? ConcluidoEm { get; set; }
}
