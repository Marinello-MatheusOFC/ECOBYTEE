using EcoByte.Web.Services;

namespace EcoByte.Tests.Services;

public class FirebaseAdminTokenValidatorTests
{
    private readonly FirebaseAdminTokenValidator _validator = new();

    [Fact]
    public async Task Validar_TokenNulo_RetornaFalhaTokenAusente()
    {
        var resultado = await _validator.ValidarAsync(null);

        Assert.False(resultado.Valido);
        Assert.Equal("Token ausente.", resultado.Erro);
    }

    [Fact]
    public async Task Validar_TokenVazio_RetornaFalhaTokenAusente()
    {
        var resultado = await _validator.ValidarAsync("   ");

        Assert.False(resultado.Valido);
        Assert.Equal("Token ausente.", resultado.Erro);
    }
}