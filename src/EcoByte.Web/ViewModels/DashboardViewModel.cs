namespace EcoByte.Web.ViewModels;

using EcoByte.Web.Enums;

public class DashboardViewModel
{
    public int TotalProdutos { get; set; }
    public int ProdutosDisponiveis { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalEstabelecimentos { get; set; }
    public int TotalSolicitacoes { get; set; }
    public int SolicitacoesPendentes { get; set; }
    public int SolicitacoesConcluidas { get; set; }
}
