namespace EcoByte.Web.Repositories;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;

public class EstabelecimentoRepository : IEstabelecimentoRepository
{
    private readonly FirestoreDb? _db;
    private const string Colecao = "estabelecimentos";

    public EstabelecimentoRepository(FirestoreDb? db) => _db = db;

    private CollectionReference ObterColecao()
    {
        if (_db is null) throw new FirestoreNotConfiguredException();
        return _db.Collection(Colecao);
    }

    public async Task<Estabelecimento?> ObterPorIdAsync(string id)
    {
        if (_db is null) throw new FirestoreNotConfiguredException();
        var snapshot = await _db.Collection(Colecao).Document(id).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<Estabelecimento>() : null;
    }

    public async Task<Estabelecimento?> ObterPorUsuarioAsync(string usuarioId)
    {
        var colecao = ObterColecao();
        var snapshot = await colecao
            .WhereEqualTo("usuarioResponsavelId", usuarioId)
            .Limit(1)
            .GetSnapshotAsync();

        return snapshot.Documents.FirstOrDefault()?.ConvertTo<Estabelecimento>();
    }

    public async Task<List<Estabelecimento>> ObterTodosAsync(int limite, int offset)
    {
        var colecao = ObterColecao();
        var snapshot = await colecao
            .OrderByDescending("criadoEm")
            .Offset(offset)
            .Limit(limite)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(d => d.ConvertTo<Estabelecimento>()).ToList();
    }

    public async Task<int> ContarTodosAsync()
    {
        var snapshot = await ObterColecao().GetSnapshotAsync();
        return snapshot.Count;
    }

    public async Task<string> CriarAsync(Estabelecimento estabelecimento)
    {
        var docRef = await ObterColecao().AddAsync(estabelecimento);
        return docRef.Id;
    }

    public async Task AtualizarAsync(Estabelecimento estabelecimento)
    {
        if (_db is null) throw new FirestoreNotConfiguredException();
        var docRef = _db.Collection(Colecao).Document(estabelecimento.Id!);
        estabelecimento.AtualizadoEm = Timestamp.FromDateTime(DateTime.UtcNow);
        await docRef.SetAsync(estabelecimento, SetOptions.MergeAll);
    }
}
