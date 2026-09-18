namespace EcoByte.Web.Repositories;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;

public class SolicitacaoRepository : ISolicitacaoRepository
{
    private readonly FirestoreDb? _db;
    private const string Colecao = "solicitacoes";

    public SolicitacaoRepository(FirestoreDb? db) => _db = db;

    private CollectionReference ObterColecao()
    {
        if (_db is null) throw new FirestoreNotConfiguredException();
        return _db.Collection(Colecao);
    }

    public async Task<Solicitacao?> ObterPorIdAsync(string id)
    {
        if (_db is null) throw new FirestoreNotConfiguredException();
        var snapshot = await _db.Collection(Colecao).Document(id).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<Solicitacao>() : null;
    }

    public async Task<List<Solicitacao>> ObterPorConsumidorAsync(string consumidorId, int limite, int offset)
    {
        var snapshot = await ObterColecao()
            .WhereEqualTo("consumidorId", consumidorId)
            .OrderByDescending("criadoEm")
            .Offset(offset).Limit(limite)
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Solicitacao>()).ToList();
    }

    public async Task<int> ContarPorConsumidorAsync(string consumidorId)
    {
        var snapshot = await ObterColecao()
            .WhereEqualTo("consumidorId", consumidorId)
            .GetSnapshotAsync();
        return snapshot.Count;
    }

    public async Task<List<Solicitacao>> ObterPorEstabelecimentoAsync(string estabelecimentoId, int limite, int offset)
    {
        var snapshot = await ObterColecao()
            .WhereEqualTo("estabelecimentoId", estabelecimentoId)
            .OrderByDescending("criadoEm")
            .Offset(offset).Limit(limite)
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Solicitacao>()).ToList();
    }

    public async Task<int> ContarPorEstabelecimentoAsync(string estabelecimentoId)
    {
        var snapshot = await ObterColecao()
            .WhereEqualTo("estabelecimentoId", estabelecimentoId)
            .GetSnapshotAsync();
        return snapshot.Count;
    }

    public async Task<List<Solicitacao>> ObterTodasAsync(int limite, int offset)
    {
        var snapshot = await ObterColecao()
            .OrderByDescending("criadoEm")
            .Offset(offset).Limit(limite)
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Solicitacao>()).ToList();
    }

    public async Task<int> ContarTodasAsync()
    {
        var snapshot = await ObterColecao().GetSnapshotAsync();
        return snapshot.Count;
    }

    public async Task<string> CriarAsync(Solicitacao solicitacao)
    {
        var docRef = await ObterColecao().AddAsync(solicitacao);
        return docRef.Id;
    }

    public Task<string> CriarNaTransacaoAsync(Solicitacao solicitacao, Transaction transaction)
    {
        if (_db is null) throw new FirestoreNotConfiguredException();

        var docRef = ObterColecao().Document();
        solicitacao.Id = docRef.Id;
        transaction.Create(docRef, solicitacao);
        return Task.FromResult(docRef.Id);
    }

    public async Task AtualizarAsync(Solicitacao solicitacao)
    {
        if (_db is null) throw new FirestoreNotConfiguredException();
        var docRef = _db.Collection(Colecao).Document(solicitacao.Id!);
        solicitacao.AtualizadoEm = Timestamp.FromDateTime(DateTime.UtcNow);
        await docRef.SetAsync(solicitacao, SetOptions.MergeAll);
    }
}
