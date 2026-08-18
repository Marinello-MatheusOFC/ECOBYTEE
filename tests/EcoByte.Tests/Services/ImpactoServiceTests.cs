using EcoByte.Web.Interfaces;
using EcoByte.Web.Services;
using EcoByte.Web.ViewModels;
using Moq;

namespace EcoByte.Tests.Services;

public class ImpactoServiceTests
{
    private readonly Mock<ISolicitacaoRepository> _solicitacaoRepo;
    private readonly ImpactoService _service;

    public ImpactoServiceTests()
    {
        _solicitacaoRepo = new Mock<ISolicitacaoRepository>();
        _service = new ImpactoService(_solicitacaoRepo.Object);
    }

    [Fact]
    public async Task ObterImpacto_SemSolicitacoes_RetornaZeros()
    {
        _solicitacaoRepo.Setup(r => r.ContarTodasAsync()).ReturnsAsync(0);
        _solicitacaoRepo.Setup(r => r.ObterTodasAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Web.Models.Solicitacao>());

        var resultado = await _service.ObterImpactoAsync(null);

        Assert.Equal(0, resultado.AlimentosSalvos);
        Assert.Equal(0, resultado.SolicitacoesConcluidas);
        Assert.Equal(0.0, resultado.Co2EvitadoKg);
    }

    [Fact]
    public async Task ObterImpacto_ComSolicitacoesConcluidas_CalculaCorretamente()
    {
        var solicitacoes = new List<Web.Models.Solicitacao>
        {
            new() { Quantidade = 10, Status = "Concluida", CriadoEm = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow) },
            new() { Quantidade = 5, Status = "Concluida", CriadoEm = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow) },
            new() { Quantidade = 3, Status = "Pendente", CriadoEm = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow) },
            new() { Quantidade = 2, Status = "Cancelada", CriadoEm = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow) }
        };

        _solicitacaoRepo.Setup(r => r.ContarTodasAsync()).ReturnsAsync(4);
        _solicitacaoRepo.Setup(r => r.ObterTodasAsync(4, 0))
            .ReturnsAsync(solicitacoes);

        var resultado = await _service.ObterImpactoAsync(null);

        Assert.Equal(15, resultado.AlimentosSalvos);
        Assert.Equal(2, resultado.SolicitacoesConcluidas);
    }
}
