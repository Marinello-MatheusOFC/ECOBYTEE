namespace EcoByte.Web.ViewModels;

using System.ComponentModel.DataAnnotations;
using EcoByte.Web.Enums;

public class OnboardingViewModel
{
    [Required(ErrorMessage = "Nome e obrigatorio.")]
    [StringLength(120, ErrorMessage = "Nome deve ter no maximo {1} caracteres.")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione um perfil.")]
    [Display(Name = "Perfil")]
    public PerfilUsuario? Perfil { get; set; }

    public string PerfilSelecionadoAtual { get; set; } = string.Empty;
}