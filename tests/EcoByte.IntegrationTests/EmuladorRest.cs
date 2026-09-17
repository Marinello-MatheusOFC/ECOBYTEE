using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public static class EmuladorRest
{
    public const string Projeto = "demo-ecobyte";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static string Base
        => $"http://127.0.0.1:8080/v1/projects/{Projeto}/databases/(default)/documents";

    public static Task<HttpResponseMessage> CriarAsync(
        string token, string colecao, string documentId, Dictionary<string, object> campos)
        => EnviarAsync(
            HttpMethod.Post,
            $"{Base}/{colecao}?documentId={Uri.EscapeDataString(documentId)}",
            token,
            SerializarCampos(campos));

    public static Task<HttpResponseMessage> ObterAsync(string token, string colecao, string documentId)
        => EnviarAsync(
            HttpMethod.Get,
            $"{Base}/{colecao}/{Uri.EscapeDataString(documentId)}",
            token,
            null);

    public static Task<HttpResponseMessage> AtualizarAsync(
        string token, string colecao, string documentId, Dictionary<string, object> campos)
    {
        var mascara = string.Join(
            "&",
            campos.Keys.Select(k => $"updateMask.fieldPaths={Uri.EscapeDataString(k)}"));

        return EnviarAsync(
            HttpMethod.Patch,
            $"{Base}/{colecao}/{Uri.EscapeDataString(documentId)}?{mascara}",
            token,
            SerializarCampos(campos));
    }

    public static Task<HttpResponseMessage> ExcluirAsync(string token, string colecao, string documentId)
        => EnviarAsync(
            HttpMethod.Delete,
            $"{Base}/{colecao}/{Uri.EscapeDataString(documentId)}",
            token,
            null);

    public static Task<HttpResponseMessage> CriarSubcolecaoAsync(
        string token, string caminho, string documentId, Dictionary<string, object> campos)
        => EnviarAsync(
            HttpMethod.Post,
            $"{Base}/{caminho}?documentId={Uri.EscapeDataString(documentId)}",
            token,
            SerializarCampos(campos));

    public static Task<HttpResponseMessage> ObterCaminhoAsync(string token, string caminho)
        => EnviarAsync(
            HttpMethod.Get,
            $"{Base}/{caminho}",
            token,
            null);

    private static async Task<HttpResponseMessage> EnviarAsync(
        HttpMethod metodo, string url, string token, string? json)
    {
        using var requisicao = new HttpRequestMessage(metodo, url);

        if (!string.IsNullOrWhiteSpace(token))
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (json is not null)
            requisicao.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return await Http.SendAsync(requisicao);
    }

    private static string SerializarCampos(Dictionary<string, object> campos)
    {
        var camposCodificados = campos
            .ToDictionary(kv => kv.Key, kv => (object)CodificarValor(kv.Value));

        return JsonSerializer.Serialize(new { fields = camposCodificados });
    }

    private static object CodificarValor(object valor) => valor switch
    {
        string valorString => new { stringValue = valorString },
        bool valorBool => new { booleanValue = valorBool },
        int valorInt => new { integerValue = valorInt.ToString() },
        long valorLong => new { integerValue = valorLong.ToString() },
        DateTime valorData => new { timestampValue = valorData.ToUniversalTime().ToString("o") },
        _ => throw new NotSupportedException(
            $"Tipo nao suportado para REST: {valor.GetType().Name}")
    };

    public static Dictionary<string, object> CamposUsuario(string uid, string nome, string email, string perfil)
        => new()
        {
            ["uid"] = uid,
            ["nome"] = nome,
            ["email"] = email,
            ["perfil"] = perfil,
            ["ativo"] = true,
            ["criadoEm"] = DateTime.UtcNow
        };

    public static Dictionary<string, object> CamposEstabelecimento(string usuarioResponsavelId, string nomeFantasia)
        => new()
        {
            ["usuarioResponsavelId"] = usuarioResponsavelId,
            ["nomeFantasia"] = nomeFantasia,
            ["telefone"] = "(11) 98888-0000",
            ["email"] = "estabelecimento@exemplo.com",
            ["descricao"] = "Estabelecimento de teste",
            ["endereco"] = "Rua Teste, 100",
            ["ativo"] = true,
            ["aprovado"] = false,
            ["criadoEm"] = DateTime.UtcNow
        };

    public static Dictionary<string, object> CamposProduto(string estabelecimentoId, int quantidade)
        => new()
        {
            ["nome"] = "pao integral",
            ["descricao"] = "Produto de teste",
            ["precoOriginalCentavos"] = 1000L,
            ["precoPromocionalCentavos"] = 500L,
            ["quantidadeDisponivel"] = quantidade,
            ["dataLimite"] = DateTime.UtcNow.AddDays(2),
            ["categoria"] = "Padaria",
            ["status"] = "Disponivel",
            ["ehVegano"] = true,
            ["ehSemGluten"] = false,
            ["ehSemLactose"] = false,
            ["estabelecimentoId"] = estabelecimentoId,
            ["criadoEm"] = DateTime.UtcNow
        };

    public static Dictionary<string, object> CamposSolicitacao(
        string produtoId, string consumidorId, string estabelecimentoId, int quantidade)
        => new()
        {
            ["produtoId"] = produtoId,
            ["consumidorId"] = consumidorId,
            ["estabelecimentoId"] = estabelecimentoId,
            ["quantidade"] = quantidade,
            ["status"] = "Pendente",
            ["criadoEm"] = DateTime.UtcNow,
            ["reservadoAte"] = DateTime.UtcNow.AddHours(24)
        };
}