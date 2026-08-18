namespace EcoByte.Web.Repositories;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;

public class LogRepository : ILogService
{
    private readonly FirestoreDb? _db;
    private const string Colecao = "logs";

    public LogRepository(FirestoreDb? db) => _db = db;

    public async Task RegistrarAsync(string usuarioId, string acao, string entidade, string? entidadeId, string resultado)
    {
        if (_db is null) return;

        var log = new LogAuditoria
        {
            UsuarioId = usuarioId,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Resultado = resultado,
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        await _db.Collection(Colecao).AddAsync(log);
    }
}
