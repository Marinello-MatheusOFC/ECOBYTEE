namespace EcoByte.Web.Services;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;

public class ConquistaService : IConquistaService
{
    private readonly FirestoreDb? _db;
    private readonly ISolicitacaoRepository _solicitacaoRepository;
    private const string ColConquistas = "conquistas";

    public ConquistaService(FirestoreDb? db, ISolicitacaoRepository solicitacaoRepository)
    {
        _db = db;
        _solicitacaoRepository = solicitacaoRepository;
    }

    public async Task<List<Conquista>> ObterTodasAsync()
    {
        if (_db is null) return new List<Conquista>();
        var snapshot = await _db.Collection(ColConquistas)
            .WhereEqualTo("ativa", true)
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Conquista>()).ToList();
    }

    public async Task<List<Conquista>> ObterConquistasUsuarioAsync(string usuarioUid)
    {
        if (_db is null) return new List<Conquista>();
        var snapshot = await _db.Collection("usuarios").Document(usuarioUid)
            .Collection("conquistas").GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Conquista>()).ToList();
    }

    public async Task VerificarEConcederAsync(string usuarioUid)
    {
        if (_db is null) return;

        var total = await _solicitacaoRepository.ContarPorConsumidorAsync(usuarioUid);
        var conquistas = await ObterTodasAsync();
        var jaPossui = await ObterConquistasUsuarioAsync(usuarioUid);
        var idsJaPossui = jaPossui.Select(c => c.Id).ToHashSet();

        foreach (var conquista in conquistas)
        {
            if (conquista.Id is not null && idsJaPossui.Contains(conquista.Id))
                continue;

            if (conquista.Criterio == "primeira_solicitacao" && total >= 1)
            {
                await ConcederAsync(usuarioUid, conquista);
            }
            else if (conquista.Criterio == "alimentos_salvos" && total >= conquista.Meta)
            {
                await ConcederAsync(usuarioUid, conquista);
            }
        }
    }

    private async Task ConcederAsync(string usuarioUid, Conquista conquista)
    {
        if (_db is null) return;
        var doc = _db.Collection("usuarios").Document(usuarioUid)
            .Collection("conquistas").Document(conquista.Id!);

        await doc.SetAsync(new
        {
            nome = conquista.Nome,
            descricao = conquista.Descricao,
            icone = conquista.Icone,
            criterio = conquista.Criterio,
            meta = conquista.Meta,
            desbloqueadaEm = Timestamp.FromDateTime(DateTime.UtcNow)
        });
    }
}
