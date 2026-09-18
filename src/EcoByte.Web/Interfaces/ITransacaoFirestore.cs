namespace EcoByte.Web.Interfaces;

using Google.Cloud.Firestore;

public interface ITransacaoFirestore
{
    Task<T> RodarTransacaoAsync<T>(
        Func<Transaction, Task<T>> operacao,
        CancellationToken cancellationToken = default);
}