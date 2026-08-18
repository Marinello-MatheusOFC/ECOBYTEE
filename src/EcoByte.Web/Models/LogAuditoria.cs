namespace EcoByte.Web.Models;

using Google.Cloud.Firestore;

[FirestoreData]
public class LogAuditoria
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty("usuarioId")]
    public string UsuarioId { get; set; } = string.Empty;

    [FirestoreProperty("acao")]
    public string Acao { get; set; } = string.Empty;

    [FirestoreProperty("entidade")]
    public string Entidade { get; set; } = string.Empty;

    [FirestoreProperty("entidadeId")]
    public string? EntidadeId { get; set; }

    [FirestoreProperty("resultado")]
    public string Resultado { get; set; } = string.Empty;

    [FirestoreProperty("criadoEm")]
    public Timestamp CriadoEm { get; set; }
}
