namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;

public interface IConquistaService
{
    Task<List<Conquista>> ObterConquistasUsuarioAsync(string usuarioUid);
    Task<List<Conquista>> ObterTodasAsync();
    Task VerificarEConcederAsync(string usuarioUid);
}
