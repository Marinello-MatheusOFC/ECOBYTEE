namespace EcoByte.Web.Repositories;

using EcoByte.Web.Enums;
using EcoByte.Web.Exceptions;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;

public class ProdutoRepository : IProdutoRepository
{
    private readonly FirestoreDb? _db;
    private const string ColecaoProdutos = "produtos";

    public ProdutoRepository(FirestoreDb? db)
    {
        _db = db;
    }

    private CollectionReference ObterColecao()
    {
        if (_db is null)
            throw new FirestoreNotConfiguredException();

        return _db.Collection(ColecaoProdutos);
    }

    public async Task<List<Produto>> ObterProdutosDisponiveisAsync(
        string? termo,
        string? categoria,
        bool? ehVegano,
        bool? ehSemGluten,
        bool? ehSemLactose,
        int limite,
        int offset)
    {
        var colecao = ObterColecao();
        Query query = colecao
            .WhereEqualTo("status", StatusProduto.Disponivel.ToFirestoreString())
            .WhereGreaterThan("quantidadeDisponivel", 0);

        var agora = Timestamp.FromDateTime(DateTime.UtcNow);
        query = query.WhereGreaterThanOrEqualTo("dataLimite", agora);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNormalizado = termo.Trim().ToLowerInvariant();
            query = query
                .WhereGreaterThanOrEqualTo("nome", termoNormalizado)
                .WhereLessThanOrEqualTo("nome", termoNormalizado + "\uf8ff");
        }

        if (categoria is not null)
        {
            query = query.WhereEqualTo(
                "categoria", categoria);
        }

        if (ehVegano is true)
            query = query.WhereEqualTo("ehVegano", true);

        if (ehSemGluten is true)
            query = query.WhereEqualTo("ehSemGluten", true);

        if (ehSemLactose is true)
            query = query.WhereEqualTo("ehSemLactose", true);

        query = query.Offset(offset).Limit(limite);

        var snapshot = await query.GetSnapshotAsync();

        return snapshot.Documents
            .Select(doc => doc.ConvertTo<Produto>())
            .ToList();
    }

    public async Task<int> ContarProdutosDisponiveisAsync(
        string? termo,
        string? categoria,
        bool? ehVegano,
        bool? ehSemGluten,
        bool? ehSemLactose)
    {
        var colecao = ObterColecao();
        Query query = colecao
            .WhereEqualTo("status", StatusProduto.Disponivel.ToFirestoreString())
            .WhereGreaterThan("quantidadeDisponivel", 0);

        var agora = Timestamp.FromDateTime(DateTime.UtcNow);
        query = query.WhereGreaterThanOrEqualTo("dataLimite", agora);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNormalizado = termo.Trim().ToLowerInvariant();
            query = query
                .WhereGreaterThanOrEqualTo("nome", termoNormalizado)
                .WhereLessThanOrEqualTo("nome", termoNormalizado + "\uf8ff");
        }

        if (categoria is not null)
        {
            query = query.WhereEqualTo(
                "categoria", categoria);
        }

        if (ehVegano is true)
            query = query.WhereEqualTo("ehVegano", true);

        if (ehSemGluten is true)
            query = query.WhereEqualTo("ehSemGluten", true);

        if (ehSemLactose is true)
            query = query.WhereEqualTo("ehSemLactose", true);

        var snapshot = await query.GetSnapshotAsync();
        return snapshot.Count;
    }

    public async Task<Produto?> ObterPorIdAsync(string id)
    {
        if (_db is null)
            throw new FirestoreNotConfiguredException();

        var docRef = _db.Collection(ColecaoProdutos).Document(id);
        var snapshot = await docRef.GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<Produto>() : null;
    }

    public async Task<string> CriarAsync(Produto produto)
    {
        var docRef = await ObterColecao().AddAsync(produto);
        produto.Id = docRef.Id;
        return docRef.Id;
    }

    public async Task AtualizarAsync(Produto produto)
    {
        if (_db is null)
            throw new FirestoreNotConfiguredException();

        var docRef = _db.Collection(ColecaoProdutos).Document(produto.Id!);
        produto.AtualizadoEm = Timestamp.FromDateTime(DateTime.UtcNow);
        await docRef.SetAsync(produto, SetOptions.MergeAll);
    }

    public async Task<List<Produto>> ObterPorEstabelecimentoAsync(
        string estabelecimentoId, int limite, int offset)
    {
        var snapshot = await ObterColecao()
            .WhereEqualTo("estabelecimentoId", estabelecimentoId)
            .OrderByDescending("criadoEm")
            .Offset(offset)
            .Limit(limite)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(doc => doc.ConvertTo<Produto>()).ToList();
    }

    public async Task<int> ContarPorEstabelecimentoAsync(string estabelecimentoId)
    {
        var snapshot = await ObterColecao()
            .WhereEqualTo("estabelecimentoId", estabelecimentoId)
            .GetSnapshotAsync();

        return snapshot.Count;
    }
}
