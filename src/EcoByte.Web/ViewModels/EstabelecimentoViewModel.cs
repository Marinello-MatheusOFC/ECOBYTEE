namespace EcoByte.Web.ViewModels;

using System.ComponentModel.DataAnnotations;

public class EstabelecimentoViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Nome fantasia e obrigatorio.")]
    [StringLength(150, ErrorMessage = "Nome deve ter no maximo {1} caracteres.")]
    [Display(Name = "Nome Fantasia")]
    public string NomeFantasia { get; set; } = string.Empty;

    [Display(Name = "Razao Social")]
    public string? RazaoSocial { get; set; }

    [Required(ErrorMessage = "Telefone e obrigatorio.")]
    [Phone(ErrorMessage = "Telefone invalido.")]
    [Display(Name = "Telefone")]
    public string Telefone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email e obrigatorio.")]
    [EmailAddress(ErrorMessage = "Email invalido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Descricao deve ter no maximo {1} caracteres.")]
    [Display(Name = "Descricao")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Endereco e obrigatorio.")]
    [Display(Name = "Endereco")]
    public string Endereco { get; set; } = string.Empty;
}
