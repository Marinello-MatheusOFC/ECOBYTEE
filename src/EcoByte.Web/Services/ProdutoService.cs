namespace EcoByte.Web.Services;

using EcoByte.Web.Enums;
using EcoByte.Web.Exceptions;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.ViewModels;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _repository;

    public ProdutoService(IProdutoRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ProdutoResumoViewModel>> ObterProdutosDisponiveisAsync(
        ProdutoFiltroViewModel filtro)
    {
        var offset = (filtro.Pagina - 1) * filtro.TamanhoPagina;

        var produtos = await _repository.ObterProdutosDisponiveisAsync(
            filtro.Termo,
            filtro.Categoria?.ToFirestoreString(),
            filtro.SomenteVeganos ? true : null,
            filtro.SomenteSemGluten ? true : null,
            filtro.SomenteSemLactose ? true : null,
            filtro.TamanhoPagina,
            offset);

        var viewModels = produtos
            .Select(MapearParaResumo)
            .ToList();

        return filtro.Ordenacao switch
        {
            ProdutoOrdenacao.MenorPreco => viewModels.OrderBy(p => p.PrecoPromocional).ToList(),
            ProdutoOrdenacao.MaiorPreco => viewModels.OrderByDescending(p => p.PrecoPromocional).ToList(),
            ProdutoOrdenacao.MaiorDesconto => viewModels.OrderByDescending(p => p.PercentualDesconto).ToList(),
            ProdutoOrdenacao.NomeAZ => viewModels.OrderBy(p => p.Nome).ToList(),
            _ => viewModels.OrderByDescending(p => p.CriadoEm).ToList()
        };
    }

    public async Task<int> ContarProdutosDisponiveisAsync(ProdutoFiltroViewModel filtro)
    {
        return await _repository.ContarProdutosDisponiveisAsync(
            filtro.Termo,
            filtro.Categoria?.ToFirestoreString(),
            filtro.SomenteVeganos ? true : null,
            filtro.SomenteSemGluten ? true : null,
            filtro.SomenteSemLactose ? true : null);
    }

    public async Task<ProdutoDetalhesViewModel?> ObterDetalhesAsync(string id)
    {
        var produto = await _repository.ObterPorIdAsync(id);

        if (produto is null)
            return null;

        return MapearParaDetalhes(produto);
    }

    private static ProdutoResumoViewModel MapearParaResumo(Produto produto)
    {
        var precoOriginal = CentavosParaDecimal(produto.PrecoOriginalCentavos);
        var precoPromocional = CentavosParaDecimal(produto.PrecoPromocionalCentavos);
        var percentualDesconto = CalcularPercentualDesconto(precoOriginal, precoPromocional);

        return new ProdutoResumoViewModel
        {
            Id = produto.Id ?? string.Empty,
            Nome = produto.Nome,
            DescricaoResumida = TruncarDescricao(produto.Descricao, 100),
            ImagemUrl = produto.ImagemUrl,
            Categoria = ParseCategoriaSeguro(produto.Categoria),
            PrecoOriginal = precoOriginal,
            PrecoPromocional = precoPromocional,
            PercentualDesconto = percentualDesconto,
            QuantidadeDisponivel = produto.QuantidadeDisponivel,
            DataLimite = produto.DataLimite?.ToDateTime() ?? DateTime.MinValue,
            EhVegano = produto.EhVegano,
            EhSemGluten = produto.EhSemGluten,
            EhSemLactose = produto.EhSemLactose,
            EstabelecimentoNome = string.Empty,
            EstaDisponivel = true,
            CriadoEm = produto.CriadoEm.ToDateTime()
        };
    }

    private static ProdutoDetalhesViewModel MapearParaDetalhes(Produto produto)
    {
        var precoOriginal = CentavosParaDecimal(produto.PrecoOriginalCentavos);
        var precoPromocional = CentavosParaDecimal(produto.PrecoPromocionalCentavos);
        var percentualDesconto = CalcularPercentualDesconto(precoOriginal, precoPromocional);

        return new ProdutoDetalhesViewModel
        {
            Id = produto.Id ?? string.Empty,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            ImagemUrl = produto.ImagemUrl,
            Categoria = ParseCategoriaSeguro(produto.Categoria),
            PrecoOriginal = precoOriginal,
            PrecoPromocional = precoPromocional,
            PercentualDesconto = percentualDesconto,
            QuantidadeDisponivel = produto.QuantidadeDisponivel,
            DataLimite = produto.DataLimite?.ToDateTime() ?? DateTime.MinValue,
            EhVegano = produto.EhVegano,
            EhSemGluten = produto.EhSemGluten,
            EhSemLactose = produto.EhSemLactose,
            EstabelecimentoNome = string.Empty,
            EstaDisponivel = true,
            CriadoEm = produto.CriadoEm.ToDateTime()
        };
    }

    public static decimal CentavosParaDecimal(long centavos)
        => centavos / 100m;

    public static long DecimalParaCentavos(decimal valor)
        => (long)Math.Round(valor * 100, MidpointRounding.AwayFromZero);

    public static int CalcularPercentualDesconto(decimal original, decimal promocional)
    {
        if (original <= 0 || promocional >= original)
            return 0;

        return (int)Math.Round(
            (1 - promocional / original) * 100, MidpointRounding.AwayFromZero);
    }

    private static string TruncarDescricao(string descricao, int maximo)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return string.Empty;

        return descricao.Length <= maximo
            ? descricao
            : descricao[..maximo] + "...";
    }

    private static CategoriaProduto ParseCategoriaSeguro(string categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
            return CategoriaProduto.Outros;

        return Enum.TryParse<CategoriaProduto>(categoria, true, out var result)
            ? result
            : CategoriaProduto.Outros;
    }
}
