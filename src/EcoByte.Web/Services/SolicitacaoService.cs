namespace EcoByte.Web.Services;

using EcoByte.Web.Enums;
using EcoByte.Web.Exceptions;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.ViewModels;
using Google.Cloud.Firestore;

public class SolicitacaoService : ISolicitacaoService
{
    private readonly ISolicitacaoRepository _repository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly ILogService _logService;

    public SolicitacaoService(
        ISolicitacaoRepository repository,
        IProdutoRepository produtoRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        ILogService logService)
    {
        _repository = repository;
        _produtoRepository = produtoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _logService = logService;
    }

    public async Task<string> CriarSolicitacaoAsync(string produtoId, string consumidorId, int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");

        var produto = await _produtoRepository.ObterPorIdAsync(produtoId)
            ?? throw new ProdutoNaoEncontradoException(produtoId);

        if (produto.QuantidadeDisponivel < quantidade)
            throw new InvalidOperationException("Estoque insuficiente.");

        if (produto.Status != StatusProduto.Disponivel.ToFirestoreString())
            throw new InvalidOperationException("Produto nao esta disponivel.");

        var solicitacao = new Solicitacao
        {
            ProdutoId = produtoId,
            ConsumidorId = consumidorId,
            EstabelecimentoId = produto.EstabelecimentoId,
            Quantidade = quantidade,
            Status = StatusSolicitacao.Pendente.ToString(),
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow),
            ReservadoAte = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(24))
        };

        var id = await _repository.CriarAsync(solicitacao);

        produto.QuantidadeDisponivel -= quantidade;
        if (produto.QuantidadeDisponivel == 0)
            produto.Status = StatusProduto.Esgotado.ToFirestoreString();

        await _produtoRepository.AtualizarAsync(produto);

        await _logService.RegistrarAsync(consumidorId, "Criar", "Solicitacao", id, "Sucesso");

        return id;
    }

    public async Task<Solicitacao?> ObterPorIdAsync(string id)
        => await _repository.ObterPorIdAsync(id);

    public async Task<List<SolicitacaoViewModel>> ObterPorConsumidorAsync(string consumidorId, int pagina, int tamanhoPagina)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        var solicitacoes = await _repository.ObterPorConsumidorAsync(consumidorId, tamanhoPagina, offset);
        return solicitacoes.Select(MapearParaViewModel).ToList();
    }

    public async Task<int> ContarPorConsumidorAsync(string consumidorId)
        => await _repository.ContarPorConsumidorAsync(consumidorId);

    public async Task<List<SolicitacaoViewModel>> ObterPorEstabelecimentoAsync(string estabelecimentoId, int pagina, int tamanhoPagina)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        var solicitacoes = await _repository.ObterPorEstabelecimentoAsync(estabelecimentoId, tamanhoPagina, offset);
        return solicitacoes.Select(MapearParaViewModel).ToList();
    }

    public async Task<int> ContarPorEstabelecimentoAsync(string estabelecimentoId)
        => await _repository.ContarPorEstabelecimentoAsync(estabelecimentoId);

    public async Task<List<SolicitacaoViewModel>> ObterTodasAsync(int pagina, int tamanhoPagina)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        var solicitacoes = await _repository.ObterTodasAsync(tamanhoPagina, offset);
        return solicitacoes.Select(MapearParaViewModel).ToList();
    }

    public async Task<int> ContarTodasAsync()
        => await _repository.ContarTodasAsync();

    public async Task CancelarAsync(string solicitacaoId, string usuarioUid)
    {
        var solicitacao = await _repository.ObterPorIdAsync(solicitacaoId)
            ?? throw new KeyNotFoundException("Solicitacao nao encontrada.");

        if (solicitacao.ConsumidorId != usuarioUid)
            throw new UnauthorizedAccessException("Operacao nao permitida.");

        if (solicitacao.Status == StatusSolicitacao.Cancelada.ToString()
            || solicitacao.Status == StatusSolicitacao.Concluida.ToString())
            throw new InvalidOperationException("Solicitacao nao pode ser cancelada neste estado.");

        solicitacao.Status = StatusSolicitacao.Cancelada.ToString();
        solicitacao.CanceladoEm = Timestamp.FromDateTime(DateTime.UtcNow);
        await _repository.AtualizarAsync(solicitacao);
        await _logService.RegistrarAsync(usuarioUid, "Cancelar", "Solicitacao", solicitacaoId, "Sucesso");
    }

    public async Task ConfirmarRetiradaAsync(string solicitacaoId, string usuarioUid)
    {
        var solicitacao = await _repository.ObterPorIdAsync(solicitacaoId)
            ?? throw new KeyNotFoundException("Solicitacao nao encontrada.");

        await ValidarProprietarioEstabelecimentoAsync(solicitacao, usuarioUid);

        if (solicitacao.Status == StatusSolicitacao.Cancelada.ToString()
            || solicitacao.Status == StatusSolicitacao.Concluida.ToString())
            throw new InvalidOperationException("Solicitacao nao pode ser concluida neste estado.");

        solicitacao.Status = StatusSolicitacao.Concluida.ToString();
        solicitacao.ConcluidoEm = Timestamp.FromDateTime(DateTime.UtcNow);
        await _repository.AtualizarAsync(solicitacao);
        await _logService.RegistrarAsync(usuarioUid, "Concluir", "Solicitacao", solicitacaoId, "Sucesso");
    }

    private async Task ValidarProprietarioEstabelecimentoAsync(Solicitacao solicitacao, string usuarioUid)
    {
        if (string.IsNullOrWhiteSpace(solicitacao.EstabelecimentoId))
            throw new UnauthorizedAccessException("Operacao nao permitida.");

        var estabelecimento = await _estabelecimentoRepository.ObterPorIdAsync(solicitacao.EstabelecimentoId)
            ?? throw new UnauthorizedAccessException("Operacao nao permitida.");

        if (estabelecimento.UsuarioResponsavelId != usuarioUid)
            throw new UnauthorizedAccessException("Operacao nao permitida.");
    }

    private static SolicitacaoViewModel MapearParaViewModel(Solicitacao s)
    {
        Enum.TryParse<StatusSolicitacao>(s.Status, true, out var status);

        return new SolicitacaoViewModel
        {
            Id = s.Id ?? string.Empty,
            ProdutoNome = string.Empty,
            Quantidade = s.Quantidade,
            Status = status,
            CriadoEm = s.CriadoEm.ToDateTime(),
            ReservadoAte = s.ReservadoAte?.ToDateTime(),
            ConcluidoEm = s.ConcluidoEm?.ToDateTime()
        };
    }
}
