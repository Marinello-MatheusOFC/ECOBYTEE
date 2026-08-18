namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;

public interface IProdutoRepository
{
    Task<List<Produto>> ObterProdutosDisponiveisAsync(
        string? termo,
        string? categoria,
        bool? ehVegano,
        bool? ehSemGluten,
        bool? ehSemLactose,
        int limite,
        int offset);

    Task<int> ContarProdutosDisponiveisAsync(
        string? termo,
        string? categoria,
        bool? ehVegano,
        bool? ehSemGluten,
        bool? ehSemLactose);

    Task<Produto?> ObterPorIdAsync(string id);
}
