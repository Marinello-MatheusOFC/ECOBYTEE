namespace EcoByte.Web.Services;

using EcoByte.Web.Interfaces;
using EcoByte.Web.ViewModels;

public class ImpactoService : IImpactoService
{
    private readonly ISolicitacaoRepository _solicitacaoRepository;

    private const double FatorCo2KgPor100g = 0.31;

    public ImpactoService(ISolicitacaoRepository solicitacaoRepository)
    {
        _solicitacaoRepository = solicitacaoRepository;
    }

    public async Task<ImpactoResumoViewModel> ObterImpactoAsync(string? usuarioUid)
    {
        var total = await _solicitacaoRepository.ContarTodasAsync();
        var solicitacoes = await _solicitacaoRepository.ObterTodasAsync(total > 0 ? total : 1, 0);

        var concluidas = solicitacoes
            .Where(s => s.Status == "Concluida")
            .ToList();

        var alimentosSalvos = concluidas.Sum(s => s.Quantidade);
        var co2Evitado = alimentosSalvos * 100.0 / 1000.0 * FatorCo2KgPor100g;

        var evolucao = concluidas
            .GroupBy(s => new { s.CriadoEm.ToDateTime().Year, s.CriadoEm.ToDateTime().Month })
            .Select(g => new MensalImpactoViewModel
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Quantidade = g.Sum(x => x.Quantidade),
                Valor = 0
            })
            .OrderBy(e => e.Ano).ThenBy(e => e.Mes)
            .ToList();

        return new ImpactoResumoViewModel
        {
            AlimentosSalvos = alimentosSalvos,
            ValorEconomizado = 0,
            Co2EvitadoKg = Math.Round(co2Evitado, 2),
            SolicitacoesConcluidas = concluidas.Count,
            EvolucaoMensal = evolucao
        };
    }
}
