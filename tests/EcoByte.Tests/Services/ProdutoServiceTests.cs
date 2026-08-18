using EcoByte.Web.Services;

namespace EcoByte.Tests.Services;

public class ProdutoServiceTests
{
    [Theory]
    [InlineData(1290, 12.90)]
    [InlineData(450, 4.50)]
    [InlineData(0, 0.00)]
    [InlineData(1, 0.01)]
    [InlineData(1000000, 10000.00)]
    public void CentavosParaDecimal_RetornaValorCorreto(long centavos, decimal esperado)
    {
        var resultado = ProdutoService.CentavosParaDecimal(centavos);
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(12.90, 1290)]
    [InlineData(4.50, 450)]
    [InlineData(0.00, 0)]
    [InlineData(0.01, 1)]
    [InlineData(10000.00, 1000000)]
    public void DecimalParaCentavos_RetornaValorCorreto(decimal valor, long esperado)
    {
        var resultado = ProdutoService.DecimalParaCentavos(valor);
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(10.00, 5.00, 50)]
    [InlineData(20.00, 10.00, 50)]
    [InlineData(100.00, 75.00, 25)]
    [InlineData(50.00, 35.00, 30)]
    public void CalcularPercentualDesconto_RetornaCorreto(decimal original, decimal promocional, int esperado)
    {
        var resultado = ProdutoService.CalcularPercentualDesconto(original, promocional);
        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData(10.00, 10.00)]
    [InlineData(10.00, 12.00)]
    [InlineData(0.00, 0.00)]
    public void CalcularPercentualDesconto_RetornaZero_QuandoInvalido(decimal original, decimal promocional)
    {
        var resultado = ProdutoService.CalcularPercentualDesconto(original, promocional);
        Assert.Equal(0, resultado);
    }

    [Theory]
    [InlineData(12.90, 1290)]
    [InlineData(0.01, 1)]
    [InlineData(99.99, 9999)]
    public void RoundTrip_Centavos_Decimal(decimal decimalInput, long _)
    {
        var centavos = ProdutoService.DecimalParaCentavos(decimalInput);
        var voltou = ProdutoService.CentavosParaDecimal(centavos);
        Assert.Equal(decimalInput, voltou);
    }

    [Fact]
    public void CalcularPercentualDesconto_Troco_10_Para_9()
    {
        var resultado = ProdutoService.CalcularPercentualDesconto(10.00m, 9.00m);
        Assert.Equal(10, resultado);
    }

    [Fact]
    public void CalcularPercentualDesconto_Troco_20_Para_5()
    {
        var resultado = ProdutoService.CalcularPercentualDesconto(20.00m, 5.00m);
        Assert.Equal(75, resultado);
    }
}
