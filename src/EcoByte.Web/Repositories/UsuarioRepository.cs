namespace EcoByte.Web.Repositories;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly FirestoreDb? _db;
    private const string Colecao = "usuarios";

    public UsuarioRepository(FirestoreDb? db) => _db = db;

    private CollectionReference ObterColecao()
    {
        if (_db is null) throw new FirestoreNotConfiguredException();
        return _db.Collection(Colecao);
    }

    public async Task<Usuario?> ObterPorUidAsync(string uid)
    {
        if (_db is null) throw new FirestoreNotConfiguredException();

        var docRef = _db.Collection(Colecao).Document(uid);
        var snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
            return snapshot.ConvertTo<Usuario>();

        var colecao = _db.Collection(Colecao);
        var busca = await colecao
            .WhereEqualTo("uid", uid)
            .Limit(1)
            .GetSnapshotAsync();

        return busca.Documents.FirstOrDefault()?.ConvertTo<Usuario>();
    }

    public async Task<Usuario> CriarAsync(Usuario usuario)
    {
        if (_db is null) throw new FirestoreNotConfiguredException();

        var docRef = _db.Collection(Colecao).Document(usuario.Uid);
        await docRef.SetAsync(usuario, SetOptions.MergeAll);
        usuario.Id = usuario.Uid;
        return usuario;
    }

    public async Task AtualizarAsync(Usuario usuario)
    {
        if (_db is null) throw new FirestoreNotConfiguredException();
        var docRef = _db.Collection(Colecao).Document(usuario.Id!);
        usuario.AtualizadoEm = Timestamp.FromDateTime(DateTime.UtcNow);
        await docRef.SetAsync(usuario, SetOptions.MergeAll);
    }

    public async Task<List<Usuario>> ObterTodosAsync(int limite, int offset)
    {
        var colecao = ObterColecao();
        var snapshot = await colecao
            .OrderByDescending("criadoEm")
            .Offset(offset)
            .Limit(limite)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(d => d.ConvertTo<Usuario>()).ToList();
    }

    public async Task<int> ContarTodosAsync()
    {
        var colecao = ObterColecao();
        var snapshot = await colecao.GetSnapshotAsync();
        return snapshot.Count;
    }
}
