using EcoByte.Web.Enums;
using EcoByte.Web.Extensions;
using EcoByte.Web.Models;
using EcoByte.Web.Services;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Exceptions;
using Moq;
using Google.Cloud.Firestore;

namespace EcoByte.Tests.Services;

public class SolicitacaoServiceTests
{
    private readonly Mock<ISolicitacaoRepository> _solicitacaoRepo;
    private readonly Mock<IProdutoRepository> _produtoRepo;
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepo;
    private readonly Mock<ILogService> _logService;
    private readonly SolicitacaoService _service;

    public SolicitacaoServiceTests()
    {
        _solicitacaoRepo = new Mock<ISolicitacaoRepository>();
        _produtoRepo = new Mock<IProdutoRepository>();
        _estabelecimentoRepo = new Mock<IEstabelecimentoRepository>();
        _logService = new Mock<ILogService>();
        _service = new SolicitacaoService(
            _solicitacaoRepo.Object,
            _produtoRepo.Object,
            _estabelecimentoRepo.Object,
            _logService.Object);
    }

    [Fact]
    public async Task CriarSolicitacao_QuantidadeZero_LancaExcecao()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CriarSolicitacaoAsync("prod1", "user1", 0));
    }

    [Fact]
    public async Task CriarSolicitacao_QuantidadeNegativa_LancaExcecao()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CriarSolicitacaoAsync("prod1", "user1", -1));
    }

    [Fact]
    public async Task CriarSolicitacao_ProdutoNaoEncontrado_LancaExcecao()
    {
        _produtoRepo.Setup(r => r.ObterPorIdAsync("prod1"))
            .ReturnsAsync((Produto?)null);

        await Assert.ThrowsAsync<ProdutoNaoEncontradoException>(
            () => _service.CriarSolicitacaoAsync("prod1", "user1", 1));
    }

    [Fact]
    public async Task CriarSolicitacao_EstoqueInsuficiente_LancaExcecao()
    {
        var produto = new Produto
        {
            Id = "prod1",
            QuantidadeDisponivel = 2,
            Status = StatusProduto.Disponivel.ToFirestoreString()
        };
        _produtoRepo.Setup(r => r.ObterPorIdAsync("prod1"))
            .ReturnsAsync(produto);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CriarSolicitacaoAsync("prod1", "user1", 5));
    }

    [Fact]
    public async Task CriarSolicitacao_ProdutoIndisponivel_LancaExcecao()
    {
        var produto = new Produto
        {
            Id = "prod1",
            QuantidadeDisponivel = 10,
            Status = StatusProduto.Esgotado.ToFirestoreString()
        };
        _produtoRepo.Setup(r => r.ObterPorIdAsync("prod1"))
            .ReturnsAsync(produto);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CriarSolicitacaoAsync("prod1", "user1", 1));
    }

    [Fact]
    public async Task CriarSolicitacao_DadosValidos_RetornaId()
    {
        var produto = new Produto
        {
            Id = "prod1",
            QuantidadeDisponivel = 10,
            Status = StatusProduto.Disponivel.ToFirestoreString(),
            EstabelecimentoId = "estab1"
        };
        _produtoRepo.Setup(r => r.ObterPorIdAsync("prod1"))
            .ReturnsAsync(produto);
        _solicitacaoRepo.Setup(r => r.CriarAsync(It.IsAny<Solicitacao>()))
            .ReturnsAsync("sol1");

        var id = await _service.CriarSolicitacaoAsync("prod1", "user1", 2);

        Assert.Equal("sol1", id);
        _solicitacaoRepo.Verify(r => r.CriarAsync(It.IsAny<Solicitacao>()), Times.Once);
    }

    [Fact]
    public async Task Cancelar_SolicitacaoNaoPertenceAoUsuario_LancaExcecao()
    {
        var solicitacao = new Solicitacao
        {
            Id = "sol1",
            ConsumidorId = "user1",
            Status = StatusSolicitacao.Pendente.ToString()
        };
        _solicitacaoRepo.Setup(r => r.ObterPorIdAsync("sol1"))
            .ReturnsAsync(solicitacao);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CancelarAsync("sol1", "user2"));
    }
}
