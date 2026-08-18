namespace EcoByte.Web.Services;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;

public class EstabelecimentoService : IEstabelecimentoService
{
    private readonly IEstabelecimentoRepository _repository;

    public EstabelecimentoService(IEstabelecimentoRepository repository) => _repository = repository;

    public async Task<Estabelecimento?> ObterPorIdAsync(string id)
        => await _repository.ObterPorIdAsync(id);

    public async Task<Estabelecimento?> ObterPorUsuarioAsync(string usuarioId)
        => await _repository.ObterPorUsuarioAsync(usuarioId);

    public async Task<string> CriarAsync(string usuarioId, string nomeFantasia, string telefone, string email, string descricao, string endereco)
    {
        var existente = await _repository.ObterPorUsuarioAsync(usuarioId);
        if (existente is not null)
            throw new InvalidOperationException("Usuario ja possui um estabelecimento.");

        var estabelecimento = new Estabelecimento
        {
            UsuarioResponsavelId = usuarioId,
            NomeFantasia = nomeFantasia,
            Telefone = telefone,
            Email = email,
            Descricao = descricao,
            Endereco = endereco,
            Ativo = true,
            Aprovado = false,
            CriadoEm = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow)
        };

        return await _repository.CriarAsync(estabelecimento);
    }

    public async Task AtualizarAsync(string id, string nomeFantasia, string telefone, string email, string descricao, string endereco)
    {
        var estabelecimento = await _repository.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException("Estabelecimento nao encontrado.");

        estabelecimento.NomeFantasia = nomeFantasia;
        estabelecimento.Telefone = telefone;
        estabelecimento.Email = email;
        estabelecimento.Descricao = descricao;
        estabelecimento.Endereco = endereco;
        await _repository.AtualizarAsync(estabelecimento);
    }

    public async Task<List<Estabelecimento>> ObterTodosAsync(int pagina, int tamanhoPagina)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        return await _repository.ObterTodosAsync(tamanhoPagina, offset);
    }

    public async Task<int> ContarTodosAsync()
        => await _repository.ContarTodosAsync();
}
