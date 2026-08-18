namespace EcoByte.Web.Models;

using Google.Cloud.Firestore;

[FirestoreData]
public class Estabelecimento
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty("usuarioResponsavelId")]
    public string UsuarioResponsavelId { get; set; } = string.Empty;

    [FirestoreProperty("nomeFantasia")]
    public string NomeFantasia { get; set; } = string.Empty;

    [FirestoreProperty("razaoSocial")]
    public string? RazaoSocial { get; set; }

    [FirestoreProperty("telefone")]
    public string Telefone { get; set; } = string.Empty;

    [FirestoreProperty("email")]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [FirestoreProperty("endereco")]
    public string Endereco { get; set; } = string.Empty;

    [FirestoreProperty("ativo")]
    public bool Ativo { get; set; } = true;

    [FirestoreProperty("aprovado")]
    public bool Aprovado { get; set; }

    [FirestoreProperty("criadoEm")]
    public Timestamp CriadoEm { get; set; }

    [FirestoreProperty("atualizadoEm")]
    public Timestamp? AtualizadoEm { get; set; }
}
