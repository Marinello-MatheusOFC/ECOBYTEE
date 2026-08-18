namespace EcoByte.Web.ViewModels;

using System.ComponentModel.DataAnnotations;

public class PerfilEditarViewModel
{
    [Required(ErrorMessage = "Nome e obrigatorio.")]
    [StringLength(120, ErrorMessage = "Nome deve ter no maximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;
}
