namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;
using Google.Cloud.Firestore;

public interface ISolicitacaoRepository
{
    Task<Solicitacao?> ObterPorIdAsync(string id);
    Task<List<Solicitacao>> ObterPorConsumidorAsync(string consumidorId, int limite, int offset);
    Task<int> ContarPorConsumidorAsync(string consumidorId);
    Task<List<Solicitacao>> ObterPorEstabelecimentoAsync(string estabelecimentoId, int limite, int offset);
    Task<int> ContarPorEstabelecimentoAsync(string estabelecimentoId);
    Task<List<Solicitacao>> ObterTodasAsync(int limite, int offset);
    Task<int> ContarTodasAsync();
    Task<string> CriarAsync(Solicitacao solicitacao);
    Task<string> CriarNaTransacaoAsync(Solicitacao solicitacao, Transaction transaction);
    Task AtualizarAsync(Solicitacao solicitacao);
}
