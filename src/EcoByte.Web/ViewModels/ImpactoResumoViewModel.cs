namespace EcoByte.Web.ViewModels;

using EcoByte.Web.Enums;

public class ImpactoResumoViewModel
{
    public int AlimentosSalvos { get; set; }
    public decimal ValorEconomizado { get; set; }
    public double Co2EvitadoKg { get; set; }
    public int SolicitacoesConcluidas { get; set; }
    public List<MensalImpactoViewModel> EvolucaoMensal { get; set; } = new();
}

public class MensalImpactoViewModel
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public string MesNome => new DateTime(Ano, Mes, 1).ToString("MMM/yyyy");
    public int Quantidade { get; set; }
    public decimal Valor { get; set; }
}
