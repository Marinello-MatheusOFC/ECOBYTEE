namespace EcoByte.Web.Models;

using Google.Cloud.Firestore;

[FirestoreData]
public class Conquista
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty("nome")]
    public string Nome { get; set; } = string.Empty;

    [FirestoreProperty("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [FirestoreProperty("icone")]
    public string? Icone { get; set; }

    [FirestoreProperty("criterio")]
    public string Criterio { get; set; } = string.Empty;

    [FirestoreProperty("meta")]
    public int Meta { get; set; }

    [FirestoreProperty("ativa")]
    public bool Ativa { get; set; } = true;
}
