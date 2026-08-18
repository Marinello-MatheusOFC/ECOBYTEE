namespace EcoByte.Web.Exceptions;

public class FirestoreNotConfiguredException : InvalidOperationException
{
    public FirestoreNotConfiguredException()
        : base("O Firestore nao esta configurado. Verifique a configuracao Firebase.Enabled.")
    {
    }
}

public class ProdutoNaoEncontradoException : KeyNotFoundException
{
    public ProdutoNaoEncontradoException(string id)
        : base($"Produto com Id '{id}' nao encontrado.")
    {
    }
}
