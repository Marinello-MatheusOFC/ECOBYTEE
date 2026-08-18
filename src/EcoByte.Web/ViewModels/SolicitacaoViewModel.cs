namespace EcoByte.Web.ViewModels;

using EcoByte.Web.Enums;

public class SolicitacaoViewModel
{
    public string Id { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;
    public string ProdutoImagemUrl { get; set; } = string.Empty;
    public string ConsumidorNome { get; set; } = string.Empty;
    public string EstabelecimentoNome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public StatusSolicitacao Status { get; set; }
    public string StatusDescricao => Status.ToString();
    public DateTime CriadoEm { get; set; }
    public DateTime? ReservadoAte { get; set; }
    public DateTime? ConcluidoEm { get; set; }
}
