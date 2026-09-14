namespace EcoByte.Web.Models.Auth;

public class ResultadoCredenciais
{
    public bool Sucesso { get; init; }
    public string? IdToken { get; init; }
    public string? Email { get; init; }
    public string? Erro { get; init; }

    public static ResultadoCredenciais Falha(string erro)
        => new() { Sucesso = false, Erro = erro };

    public static ResultadoCredenciais SucessoToken(string idToken, string email)
        => new() { Sucesso = true, IdToken = idToken, Email = email };
}