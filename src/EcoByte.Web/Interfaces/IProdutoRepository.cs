namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;
using Google.Cloud.Firestore;

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

    Task<string> CriarAsync(Produto produto);

    Task AtualizarAsync(Produto produto);

    Task<Produto?> ObterNaTransacaoAsync(string id, Transaction transaction);

    Task AtualizarNaTransacaoAsync(Produto produto, Transaction transaction);

    Task<List<Produto>> ObterPorEstabelecimentoAsync(string estabelecimentoId, int limite, int offset);

    Task<int> ContarPorEstabelecimentoAsync(string estabelecimentoId);
}
