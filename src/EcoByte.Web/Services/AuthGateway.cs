namespace EcoByte.Web.Services;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcoByte.Web.Firebase;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models.Auth;

public class AuthGateway : IAuthGateway
{
    private readonly FirebaseConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthGateway(FirebaseConfig config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public Task<ResultadoCredenciais> AutenticarComSenhaAsync(
        string email, string senha, CancellationToken cancellationToken = default)
        => ExecutarAsync("signInWithPassword", email, senha, cancellationToken);

    public Task<ResultadoCredenciais> RegistrarComSenhaAsync(
        string email, string senha, CancellationToken cancellationToken = default)
        => ExecutarAsync("signUp", email, senha, cancellationToken);

    private async Task<ResultadoCredenciais> ExecutarAsync(
        string acao, string email, string senha, CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
            return ResultadoCredenciais.Falha("Autenticacao indisponivel.");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return ResultadoCredenciais.Falha("Email e senha sao obrigatorios.");

        var baseUrl = _config.UseEmulator
            ? $"http://{_config.AuthEmulatorHost}/identitytoolkit.googleapis.com/v1/accounts:{acao}"
            : $"https://identitytoolkit.googleapis.com/v1/accounts:{acao}";

        var url = $"{baseUrl}?key={WebUtility.UrlEncode(_config.ApiKey)}";

        var payload = new Dictionary<string, object>
        {
            ["email"] = email,
            ["password"] = senha,
            ["returnSecureToken"] = true
        };

        try
        {
            var client = _httpClientFactory.CreateClient("FirebaseAuth");
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return ResultadoCredenciais.Falha(MensagemErroLegivel(content));

            using var json = JsonDocument.Parse(content);
            var raiz = json.RootElement;

            if (!raiz.TryGetProperty("idToken", out var idTokenProp)
                || string.IsNullOrWhiteSpace(idTokenProp.GetString()))
                return ResultadoCredenciais.Falha("Resposta invalida do Firebase.");

            var emailRetornado = raiz.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString()
                : email;

            return ResultadoCredenciais.SucessoToken(
                idTokenProp.GetString()!, emailRetornado ?? email);
        }
        catch (HttpRequestException)
        {
            return ResultadoCredenciais.Falha("Nao foi possivel contatar o servico de autenticacao.");
        }
        catch (TaskCanceledException)
        {
            return ResultadoCredenciais.Falha("Tempo de autenticacao esgotado.");
        }
        catch (Exception)
        {
            return ResultadoCredenciais.Falha("Erro ao autenticar.");
        }
    }

    private static string MensagemErroLegivel(string content)
    {
        try
        {
            using var json = JsonDocument.Parse(content);
            var mensagem = json.RootElement
                .GetProperty("error")
                .GetProperty("message")
                .GetString();

            if (mensagem is null)
                return "Credenciais invalidas.";

            var codigo = mensagem.Split(':')[0].ToUpperInvariant();

            return codigo switch
            {
                "EMAIL_NOT_FOUND" or "INVALID_PASSWORD" or "INVALID_LOGIN_CREDENTIALS" => "Email ou senha invalidos.",
                "EMAIL_EXISTS" => "Ja existe uma conta com este email.",
                "WEAK_PASSWORD" => "Senha muito fraca.",
                "INVALID_EMAIL" => "Email invalido.",
                "USER_DISABLED" => "Conta desativada.",
                _ => "Nao foi possivel autenticar. Tente novamente."
            };
        }
        catch (Exception)
        {
            return "Nao foi possivel autenticar. Tente novamente.";
        }
    }
}