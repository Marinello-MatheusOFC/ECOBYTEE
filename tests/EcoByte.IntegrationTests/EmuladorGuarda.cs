namespace EcoByte.IntegrationTests;

public static class EmuladorGuarda
{
    public const string VariavelHabilitacao = "ECOBYTE_EXECUTA_TESTES_EMULADOR";

    public static bool Habilitado =>
        Environment.GetEnvironmentVariable(VariavelHabilitacao) == "1";

    public static void SairSeNaoHabilitado()
    {
        Skip.If(
            !Habilitado,
            "Emulador Firebase indisponivel. Instale o Firebase CLI, rode scripts/emuladores.ps1 e defina " +
            VariavelHabilitacao + "=1.");
    }
}