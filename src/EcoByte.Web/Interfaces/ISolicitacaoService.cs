namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;
using EcoByte.Web.ViewModels;

public interface ISolicitacaoService
{
    Task<string> CriarSolicitacaoAsync(string produtoId, string consumidorId, int quantidade);
    Task<Solicitacao?> ObterPorIdAsync(string id);
    Task<List<SolicitacaoViewModel>> ObterPorConsumidorAsync(string consumidorId, int pagina, int tamanhoPagina);
    Task<int> ContarPorConsumidorAsync(string consumidorId);
    Task<List<SolicitacaoViewModel>> ObterPorEstabelecimentoAsync(string estabelecimentoId, int pagina, int tamanhoPagina);
    Task<int> ContarPorEstabelecimentoAsync(string estabelecimentoId);
    Task<List<SolicitacaoViewModel>> ObterTodasAsync(int pagina, int tamanhoPagina);
    Task<int> ContarTodasAsync();
    Task CancelarAsync(string solicitacaoId, string usuarioUid);
    Task ConfirmarRetiradaAsync(string solicitacaoId, string usuarioUid);
}
