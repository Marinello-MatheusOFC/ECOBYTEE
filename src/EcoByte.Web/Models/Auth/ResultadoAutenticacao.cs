namespace EcoByte.Web.Models.Auth;

public class ResultadoAutenticacao
{
    public bool Sucesso { get; init; }
    public string? Uid { get; init; }
    public string? Nome { get; init; }
    public string? Email { get; init; }
    public bool EmailVerificado { get; init; }
    public string? Perfil { get; init; }
    public string? EstabelecimentoId { get; init; }
    public bool PossuiPerfil { get; init; }
    public string? Erro { get; init; }

    public static ResultadoAutenticacao Falha(string erro)
        => new() { Sucesso = false, Erro = erro };

    public static ResultadoAutenticacao SucessoAutenticacao(
        string uid, string email, bool emailVerificado,
        string? nome, string? perfil, bool possuiPerfil,
        string? estabelecimentoId = null)
        => new()
        {
            Sucesso = true,
            Uid = uid,
            Email = email,
            EmailVerificado = emailVerificado,
            Nome = nome,
            Perfil = perfil,
            PossuiPerfil = possuiPerfil,
            EstabelecimentoId = estabelecimentoId
        };
}