namespace EcoByte.Web.Services;

using EcoByte.Web.Exceptions;
using EcoByte.Web.Interfaces;
using Google.Cloud.Firestore;

public class TransacaoFirestore : ITransacaoFirestore
{
    private readonly FirestoreDb? _db;

    public TransacaoFirestore(FirestoreDb? db) => _db = db;

    public Task<T> RodarTransacaoAsync<T>(
        Func<Transaction, Task<T>> operacao,
        CancellationToken cancellationToken = default)
    {
        if (_db is null)
            throw new FirestoreNotConfiguredException();

        return _db.RunTransactionAsync(operacao, cancellationToken: cancellationToken);
    }
}