namespace EcoByte.Web.Extensions;

using EcoByte.Web.Enums;

public static class EnumFirestoreExtensions
{
    public static string ToFirestoreString(this CategoriaProduto categoria)
        => categoria switch
        {
            CategoriaProduto.Padaria => "Padaria",
            CategoriaProduto.Mercearia => "Mercearia",
            CategoriaProduto.RefeicaoPronta => "RefeicaoPronta",
            CategoriaProduto.Laticinios => "Laticinios",
            CategoriaProduto.Hortifruti => "Hortifruti",
            CategoriaProduto.Bebidas => "Bebidas",
            CategoriaProduto.Outros => "Outros",
            _ => "Outros"
        };

    public static string ToFirestoreString(this StatusProduto status)
        => status switch
        {
            StatusProduto.Rascunho => "Rascunho",
            StatusProduto.Disponivel => "Disponivel",
            StatusProduto.Reservado => "Reservado",
            StatusProduto.Esgotado => "Esgotado",
            StatusProduto.Expirado => "Expirado",
            StatusProduto.Desativado => "Desativado",
            _ => "Desconhecido"
        };

    public static CategoriaProduto? ParseCategoria(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<CategoriaProduto>(value, true, out var result)
            ? result
            : null;
    }
}
