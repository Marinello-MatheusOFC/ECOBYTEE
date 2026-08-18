namespace EcoByte.Web.ViewModels;

using System.ComponentModel.DataAnnotations;
using EcoByte.Web.Enums;

public class ProdutoFiltroViewModel
{
    public string? Termo { get; set; }
    public CategoriaProduto? Categoria { get; set; }
    public bool SomenteVeganos { get; set; }
    public bool SomenteSemGluten { get; set; }
    public bool SomenteSemLactose { get; set; }
    public ProdutoOrdenacao Ordenacao { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 12;
    public int TotalItens { get; set; }
    public int TotalPaginas => TotalItens > 0
        ? (int)Math.Ceiling((double)TotalItens / TamanhoPagina)
        : 0;
    public bool TemPaginaAnterior => Pagina > 1;
    public bool TemProximaPagina => Pagina < TotalPaginas;
}

public enum ProdutoOrdenacao
{
    [Display(Name = "Mais recente")]
    MaisRecente,

    [Display(Name = "Menor preco")]
    MenorPreco,

    [Display(Name = "Maior preco")]
    MaiorPreco,

    [Display(Name = "Maior desconto")]
    MaiorDesconto,

    [Display(Name = "Nome A-Z")]
    NomeAZ
}
