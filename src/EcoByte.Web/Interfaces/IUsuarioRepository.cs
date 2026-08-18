namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorUidAsync(string uid);
    Task<Usuario> CriarAsync(Usuario usuario);
    Task AtualizarAsync(Usuario usuario);
    Task<List<Usuario>> ObterTodosAsync(int limite, int offset);
    Task<int> ContarTodosAsync();
}
