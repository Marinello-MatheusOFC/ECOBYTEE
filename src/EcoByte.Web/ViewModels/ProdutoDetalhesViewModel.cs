namespace EcoByte.Web.ViewModels;

using EcoByte.Web.Enums;

public class ProdutoDetalhesViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? ImagemUrl { get; set; }
    public CategoriaProduto Categoria { get; set; }
    public string CategoriaDescricao => Categoria == CategoriaProduto.Desconhecida
        ? "Outros"
        : Categoria.ToString();
    public decimal PrecoOriginal { get; set; }
    public decimal PrecoPromocional { get; set; }
    public int PercentualDesconto { get; set; }
    public decimal Economia => PrecoOriginal - PrecoPromocional;
    public int QuantidadeDisponivel { get; set; }
    public DateTime DataLimite { get; set; }
    public bool EhVegano { get; set; }
    public bool EhSemGluten { get; set; }
    public bool EhSemLactose { get; set; }
    public string EstabelecimentoNome { get; set; } = string.Empty;
    public bool EstaDisponivel { get; set; }
    public string TextoDisponibilidade => !EstaDisponivel
        ? "Indisponivel"
        : QuantidadeDisponivel <= 5
            ? $"Ultimas {QuantidadeDisponivel} unidades"
            : $"{QuantidadeDisponivel} unidades disponiveis";
    public DateTime CriadoEm { get; set; }
}
