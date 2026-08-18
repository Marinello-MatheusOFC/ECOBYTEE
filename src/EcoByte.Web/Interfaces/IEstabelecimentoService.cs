namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;

public interface IEstabelecimentoService
{
    Task<Estabelecimento?> ObterPorIdAsync(string id);
    Task<Estabelecimento?> ObterPorUsuarioAsync(string usuarioId);
    Task<string> CriarAsync(string usuarioId, string nomeFantasia, string telefone, string email, string descricao, string endereco);
    Task AtualizarAsync(string id, string nomeFantasia, string telefone, string email, string descricao, string endereco);
    Task<List<Estabelecimento>> ObterTodosAsync(int pagina, int tamanhoPagina);
    Task<int> ContarTodosAsync();
}
