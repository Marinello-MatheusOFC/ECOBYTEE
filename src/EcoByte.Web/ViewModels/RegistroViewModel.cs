namespace EcoByte.Web.ViewModels;

using System.ComponentModel.DataAnnotations;

public class RegistroViewModel
{
    [Required(ErrorMessage = "Nome e obrigatorio.")]
    [StringLength(120, ErrorMessage = "Nome deve ter no maximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email e obrigatorio.")]
    [EmailAddress(ErrorMessage = "Email invalido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha e obrigatoria.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter entre {2} e {1} caracteres.")]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmacao de senha e obrigatoria.")]
    [Compare("Senha", ErrorMessage = "As senhas nao conferem.")]
    [Display(Name = "Confirmar Senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
