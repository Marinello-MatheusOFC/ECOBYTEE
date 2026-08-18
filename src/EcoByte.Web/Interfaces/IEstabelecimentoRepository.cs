namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;

public interface IEstabelecimentoRepository
{
    Task<Estabelecimento?> ObterPorIdAsync(string id);
    Task<Estabelecimento?> ObterPorUsuarioAsync(string usuarioId);
    Task<List<Estabelecimento>> ObterTodosAsync(int limite, int offset);
    Task<int> ContarTodosAsync();
    Task<string> CriarAsync(Estabelecimento estabelecimento);
    Task AtualizarAsync(Estabelecimento estabelecimento);
}
