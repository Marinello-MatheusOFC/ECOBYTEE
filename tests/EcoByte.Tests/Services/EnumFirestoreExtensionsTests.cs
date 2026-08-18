using EcoByte.Web.Enums;
using EcoByte.Web.Extensions;

namespace EcoByte.Tests.Services;

public class EnumFirestoreExtensionsTests
{
    [Theory]
    [InlineData(CategoriaProduto.Padaria, "Padaria")]
    [InlineData(CategoriaProduto.Mercearia, "Mercearia")]
    [InlineData(CategoriaProduto.RefeicaoPronta, "RefeicaoPronta")]
    [InlineData(CategoriaProduto.Laticinios, "Laticinios")]
    [InlineData(CategoriaProduto.Hortifruti, "Hortifruti")]
    [InlineData(CategoriaProduto.Bebidas, "Bebidas")]
    [InlineData(CategoriaProduto.Outros, "Outros")]
    [InlineData(CategoriaProduto.Desconhecida, "Outros")]
    public void ToFirestoreString_Categoria_RetornaStringCorreta(CategoriaProduto categoria, string esperado)
    {
        Assert.Equal(esperado, categoria.ToFirestoreString());
    }

    [Theory]
    [InlineData(StatusProduto.Rascunho, "Rascunho")]
    [InlineData(StatusProduto.Disponivel, "Disponivel")]
    [InlineData(StatusProduto.Reservado, "Reservado")]
    [InlineData(StatusProduto.Esgotado, "Esgotado")]
    [InlineData(StatusProduto.Expirado, "Expirado")]
    [InlineData(StatusProduto.Desativado, "Desativado")]
    [InlineData(StatusProduto.Desconhecido, "Desconhecido")]
    public void ToFirestoreString_Status_RetornaStringCorreta(StatusProduto status, string esperado)
    {
        Assert.Equal(esperado, status.ToFirestoreString());
    }

    [Theory]
    [InlineData("Padaria", CategoriaProduto.Padaria)]
    [InlineData("Mercearia", CategoriaProduto.Mercearia)]
    [InlineData("Hortifruti", CategoriaProduto.Hortifruti)]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Invalida", null)]
    public void ParseCategoria_RetornaCorreto(string? input, CategoriaProduto? esperado)
    {
        var resultado = EnumFirestoreExtensions.ParseCategoria(input);
        Assert.Equal(esperado, resultado);
    }
}
