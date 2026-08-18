namespace EcoByte.Web.Models;

using Google.Cloud.Firestore;

[FirestoreData]
public class Produto
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty("nome")]
    public string Nome { get; set; } = string.Empty;

    [FirestoreProperty("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [FirestoreProperty("precoOriginalCentavos")]
    public long PrecoOriginalCentavos { get; set; }

    [FirestoreProperty("precoPromocionalCentavos")]
    public long PrecoPromocionalCentavos { get; set; }

    [FirestoreProperty("quantidadeDisponivel")]
    public int QuantidadeDisponivel { get; set; }

    [FirestoreProperty("dataLimite")]
    public Timestamp? DataLimite { get; set; }

    [FirestoreProperty("imagemUrl")]
    public string? ImagemUrl { get; set; }

    [FirestoreProperty("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [FirestoreProperty("status")]
    public string Status { get; set; } = string.Empty;

    [FirestoreProperty("ehVegano")]
    public bool EhVegano { get; set; }

    [FirestoreProperty("ehSemGluten")]
    public bool EhSemGluten { get; set; }

    [FirestoreProperty("ehSemLactose")]
    public bool EhSemLactose { get; set; }

    [FirestoreProperty("estabelecimentoId")]
    public string EstabelecimentoId { get; set; } = string.Empty;

    [FirestoreProperty("criadoEm")]
    public Timestamp CriadoEm { get; set; }

    [FirestoreProperty("atualizadoEm")]
    public Timestamp? AtualizadoEm { get; set; }
}
