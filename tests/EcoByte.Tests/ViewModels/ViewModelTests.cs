using EcoByte.Web.ViewModels;
using EcoByte.Web.Enums;

namespace EcoByte.Tests.ViewModels;

public class ProdutoFiltroViewModelTests
{
    [Fact]
    public void TotalPaginas_RetornaZero_QuandoZeroItens()
    {
        var filtro = new ProdutoFiltroViewModel { TotalItens = 0, TamanhoPagina = 12 };
        Assert.Equal(0, filtro.TotalPaginas);
    }

    [Theory]
    [InlineData(1, 12, 1)]
    [InlineData(12, 12, 1)]
    [InlineData(13, 12, 2)]
    [InlineData(24, 12, 2)]
    [InlineData(25, 12, 3)]
    [InlineData(100, 10, 10)]
    public void TotalPaginas_CalculaCorretamente(int totalItens, int tamanhoPagina, int esperado)
    {
        var filtro = new ProdutoFiltroViewModel
        {
            TotalItens = totalItens,
            TamanhoPagina = tamanhoPagina
        };
        Assert.Equal(esperado, filtro.TotalPaginas);
    }

    [Fact]
    public void TemPaginaAnterior_Falso_NaPagina1()
    {
        var filtro = new ProdutoFiltroViewModel { Pagina = 1 };
        Assert.False(filtro.TemPaginaAnterior);
    }

    [Fact]
    public void TemPaginaAnterior_Verdadeiro_NaPagina2()
    {
        var filtro = new ProdutoFiltroViewModel { Pagina = 2 };
        Assert.True(filtro.TemPaginaAnterior);
    }

    [Fact]
    public void TemProximaPagina_Falso_NaUltimaPagina()
    {
        var filtro = new ProdutoFiltroViewModel
        {
            Pagina = 2,
            TotalItens = 20,
            TamanhoPagina = 12
        };
        Assert.False(filtro.TemProximaPagina);
    }

    [Fact]
    public void TemProximaPagina_Verdadeiro_NaoEhUltima()
    {
        var filtro = new ProdutoFiltroViewModel
        {
            Pagina = 1,
            TotalItens = 25,
            TamanhoPagina = 12
        };
        Assert.True(filtro.TemProximaPagina);
    }
}

public class ProdutoResumoViewModelTests
{
    [Fact]
    public void CategoriaDescricao_RetornaOutros_QuandoDesconhecida()
    {
        var vm = new ProdutoResumoViewModel { Categoria = CategoriaProduto.Desconhecida };
        Assert.Equal("Outros", vm.CategoriaDescricao);
    }

    [Theory]
    [InlineData(CategoriaProduto.Padaria)]
    [InlineData(CategoriaProduto.Bebidas)]
    [InlineData(CategoriaProduto.Hortifruti)]
    public void CategoriaDescricao_RetornaNomeDaCategoria(CategoriaProduto cat)
    {
        var vm = new ProdutoResumoViewModel { Categoria = cat };
        Assert.Equal(cat.ToString(), vm.CategoriaDescricao);
    }

    [Fact]
    public void TextoDisponibilidade_Indisponivel()
    {
        var vm = new ProdutoResumoViewModel { EstaDisponivel = false, QuantidadeDisponivel = 10 };
        Assert.Equal("Indisponivel", vm.TextoDisponibilidade);
    }

    [Fact]
    public void TextoDisponibilidade_UltimasUnidades()
    {
        var vm = new ProdutoResumoViewModel { EstaDisponivel = true, QuantidadeDisponivel = 3 };
        Assert.Contains("Ultimas", vm.TextoDisponibilidade);
    }

    [Fact]
    public void TextoDisponibilidade_UnidadesDisponiveis()
    {
        var vm = new ProdutoResumoViewModel { EstaDisponivel = true, QuantidadeDisponivel = 20 };
        Assert.Contains("20 unidades", vm.TextoDisponibilidade);
    }
}

public class ProdutoDetalhesViewModelTests
{
    [Fact]
    public void Economia_CalculaCorretamente()
    {
        var vm = new ProdutoDetalhesViewModel
        {
            PrecoOriginal = 20.00m,
            PrecoPromocional = 15.00m
        };
        Assert.Equal(5.00m, vm.Economia);
    }

    [Fact]
    public void PercentualDesconto_RetornaZero_QuandoSemDesconto()
    {
        var vm = new ProdutoDetalhesViewModel
        {
            PrecoOriginal = 10.00m,
            PrecoPromocional = 10.00m
        };
        Assert.Equal(0, vm.PercentualDesconto);
    }
}
