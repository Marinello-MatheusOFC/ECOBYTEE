namespace EcoByte.Web.Models;

using Google.Cloud.Firestore;

[FirestoreData]
public class Solicitacao
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty("produtoId")]
    public string ProdutoId { get; set; } = string.Empty;

    [FirestoreProperty("consumidorId")]
    public string ConsumidorId { get; set; } = string.Empty;

    [FirestoreProperty("estabelecimentoId")]
    public string EstabelecimentoId { get; set; } = string.Empty;

    [FirestoreProperty("quantidade")]
    public int Quantidade { get; set; }

    [FirestoreProperty("status")]
    public string Status { get; set; } = "Pendente";

    [FirestoreProperty("criadoEm")]
    public Timestamp CriadoEm { get; set; }

    [FirestoreProperty("atualizadoEm")]
    public Timestamp? AtualizadoEm { get; set; }

    [FirestoreProperty("reservadoAte")]
    public Timestamp? ReservadoAte { get; set; }

    [FirestoreProperty("concluidoEm")]
    public Timestamp? ConcluidoEm { get; set; }

    [FirestoreProperty("canceladoEm")]
    public Timestamp? CanceladoEm { get; set; }

    [FirestoreProperty("responsavelRetiradaId")]
    public string? ResponsavelRetiradaId { get; set; }
}
