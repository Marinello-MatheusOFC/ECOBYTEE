namespace EcoByte.Web.ViewModels;

using System.ComponentModel.DataAnnotations;
using EcoByte.Web.Enums;

public class ProdutoFormularioViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Nome e obrigatorio.")]
    [StringLength(120, ErrorMessage = "Nome deve ter no maximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Descricao deve ter no maximo {1} caracteres.")]
    [Display(Name = "Descricao")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Preco original e obrigatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Preco original deve ser maior que zero.")]
    [Display(Name = "Preco Original (R$)")]
    public decimal PrecoOriginal { get; set; }

    [Required(ErrorMessage = "Preco promocional e obrigatorio.")]
    [Range(0, double.MaxValue, ErrorMessage = "Preco promocional nao pode ser negativo.")]
    [Display(Name = "Preco Promocional (R$)")]
    public decimal PrecoPromocional { get; set; }

    [Required(ErrorMessage = "Quantidade e obrigatoria.")]
    [Range(0, int.MaxValue, ErrorMessage = "Quantidade nao pode ser negativa.")]
    [Display(Name = "Quantidade Disponivel")]
    public int QuantidadeDisponivel { get; set; }

    [Required(ErrorMessage = "Data limite e obrigatoria.")]
    [Display(Name = "Data Limite")]
    public DateTime DataLimite { get; set; }

    [Display(Name = "URL da Imagem")]
    public string? ImagemUrl { get; set; }

    [Required(ErrorMessage = "Categoria e obrigatoria.")]
    [Display(Name = "Categoria")]
    public CategoriaProduto Categoria { get; set; }

    [Display(Name = "Vegano")]
    public bool EhVegano { get; set; }

    [Display(Name = "Sem Gluten")]
    public bool EhSemGluten { get; set; }

    [Display(Name = "Sem Lactose")]
    public bool EhSemLactose { get; set; }
}
