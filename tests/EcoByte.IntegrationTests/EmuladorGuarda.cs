namespace EcoByte.IntegrationTests;

public static class AmbienteEmulador
{
    public const string Projeto = "ecobyte-testes";
    public const string ProjetoAuthAdmin = "demo-ecobyte";
    public const string HostAuth = "127.0.0.1:9099";
    public const string HostFirestore = "127.0.0.1:8080";
    public const string HostStorage = "127.0.0.1:9199";

    public static void ConfigurarAmbienteSeguro()
    {
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", HostFirestore);
        Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", HostAuth);
        Environment.SetEnvironmentVariable("FIREBASE_STORAGE_EMULATOR_HOST", HostStorage);
        Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", Projeto);
    }
}

public static class EmuladorGuarda
{
    public const string VariavelHabilitacao = "ECOBYTE_EXECUTA_TESTES_EMULADOR";

    public static bool Habilitado =>
        Environment.GetEnvironmentVariable(VariavelHabilitacao) == "1";

    /// <summary>
    /// Seguranca dos testes de integracao:
    /// - desabilitado -> Skip;
    /// - habilitado mas host de emulador ausente ou apontando para servico externo -> falha de forma segura.
    /// Nunca permite conexao accidental com Firebase de producao.
    /// </summary>
    public static void Validar()
    {
        if (!Habilitado)
        {
            Skip.If(
                true,
                "Emulador Firebase indisponivel. Rode scripts/emuladores.ps1 e defina " +
                VariavelHabilitacao + "=1.");
            return;
        }

        ValidarHost("FIRESTORE_EMULATOR_HOST");
        ValidarHost("FIREBASE_AUTH_EMULATOR_HOST");
        ValidarHost("FIREBASE_STORAGE_EMULATOR_HOST");

        var projeto = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
        if (string.IsNullOrWhiteSpace(projeto) || projeto.Contains("prod", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "GOOGLE_CLOUD_PROJECT ausente ou suspeito de producao; abortando testes de integracao.");
        }
    }

    private static void ValidarHost(string variavel)
    {
        var valor = Environment.GetEnvironmentVariable(variavel);

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                $"{variavel} nao definida com {VariavelHabilitacao}=1; abortando para proteger producao.");
        }

        if (!EhHostLocal(valor))
        {
            throw new InvalidOperationException(
                $"{variavel}={valor} aponta para servico externo; abortando para proteger producao.");
        }
    }

    private static bool EhHostLocal(string hostComPorta)
    {
        var host = hostComPorta.Split(':')[0].Trim('[', ']');
        return host is "127.0.0.1" or "localhost" or "::1" or "0.0.0.0";
    }
}