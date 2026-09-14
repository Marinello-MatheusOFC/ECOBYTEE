namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;

public interface IUsuarioService
{
    Task<Usuario?> ObterPorUidAsync(string uid);
    Task<Usuario> ObouCriarAsync(string uid, string nome, string email);
    Task AtualizarPerfilAsync(string uid, string nome);
    Task DefinirPerfilAsync(string uid, string email, string nome, string perfil);
    Task<List<Usuario>> ObterTodosAsync(int pagina, int tamanhoPagina);
    Task<int> ContarTodosAsync();
}
