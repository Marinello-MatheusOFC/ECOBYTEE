using System.Net.Http.Headers;
using System.Text;

public static class EmuladorStorage
{
    public const string Bucket = "demo-ecobyte.appspot.com";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static string Url
        => $"http://127.0.0.1:9199/v0/b/{Bucket}/o";

    public static async Task<HttpResponseMessage> EnviarAsync(
        string token, string caminho, byte[] bytes, string contentType)
    {
        using var requisicao = new HttpRequestMessage(
            HttpMethod.Post,
            $"{Url}?uploadType=media&name={Uri.EscapeDataString(caminho)}");

        if (!string.IsNullOrWhiteSpace(token))
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        requisicao.Content = new ByteArrayContent(bytes);
        requisicao.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return await Http.SendAsync(requisicao);
    }

    public static Task<HttpResponseMessage> EnviarPngAsync(string token, string caminho, int tamanhoBytes = 1378)
        => EnviarAsync(token, caminho, BytesPng(tamanhoBytes), "image/png");

    public static Task<HttpResponseMessage> EnviarTextoAsync(string token, string caminho)
        => EnviarAsync(token, caminho, Encoding.UTF8.GetBytes("nao sou uma imagem"), "text/plain");

    public static async Task<HttpResponseMessage> ExcluirAsync(string token, string caminho)
    {
        using var requisicao = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{Url}/{Uri.EscapeDataString(caminho)}");

        if (!string.IsNullOrWhiteSpace(token))
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await Http.SendAsync(requisicao);
    }

    public static byte[] BytesPng(int tamanhoBytes)
    {
        var bytes = new byte[tamanhoBytes];
        var cabecalho = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Array.Copy(cabecalho, bytes, cabecalho.Length);
        for (var i = cabecalho.Length; i < bytes.Length; i++)
            bytes[i] = 0x00;
        return bytes;
    }
}