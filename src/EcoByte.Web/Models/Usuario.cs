namespace EcoByte.Web.Models;

using Google.Cloud.Firestore;

[FirestoreData]
public class Usuario
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty("uid")]
    public string Uid { get; set; } = string.Empty;

    [FirestoreProperty("nome")]
    public string Nome { get; set; } = string.Empty;

    [FirestoreProperty("email")]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty("perfil")]
    public string Perfil { get; set; } = "Consumidor";

    [FirestoreProperty("ativo")]
    public bool Ativo { get; set; } = true;

    [FirestoreProperty("criadoEm")]
    public Timestamp CriadoEm { get; set; }

    [FirestoreProperty("atualizadoEm")]
    public Timestamp? AtualizadoEm { get; set; }
}
