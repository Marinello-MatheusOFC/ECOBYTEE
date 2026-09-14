namespace EcoByte.Web.Models.Auth;

public class ValidacaoTokenResultado
{
    public bool Valido { get; init; }
    public string? Uid { get; init; }
    public string? Email { get; init; }
    public bool EmailVerificado { get; init; }
    public string? Erro { get; init; }

    public static ValidacaoTokenResultado Falha(string erro)
        => new() { Valido = false, Erro = erro };

    public static ValidacaoTokenResultado Sucesso(
        string uid, string? email, bool emailVerificado)
        => new()
        {
            Valido = true,
            Uid = uid,
            Email = email,
            EmailVerificado = emailVerificado
        };
}