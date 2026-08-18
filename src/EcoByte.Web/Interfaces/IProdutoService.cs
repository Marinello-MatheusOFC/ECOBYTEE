namespace EcoByte.Web.Interfaces;

using EcoByte.Web.ViewModels;

public interface IProdutoService
{
    Task<List<ProdutoResumoViewModel>> ObterProdutosDisponiveisAsync(
        ProdutoFiltroViewModel filtro);

    Task<int> ContarProdutosDisponiveisAsync(ProdutoFiltroViewModel filtro);

    Task<ProdutoDetalhesViewModel?> ObterDetalhesAsync(string id);
}
