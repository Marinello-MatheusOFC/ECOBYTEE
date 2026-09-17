using EcoByte.IntegrationTests;
using Google.Api.Gax;
using Google.Cloud.Firestore;

public class FirestoreIntegrationTests
{
    static FirestoreIntegrationTests()
    {
        AmbienteEmulador.ConfigurarAmbienteSeguro();
    }

    private static FirestoreDb CriarDb()
    {
        EmuladorGuarda.Validar();
        return new FirestoreDbBuilder
        {
            ProjectId = AmbienteEmulador.Projeto,
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();
    }

    [SkippableFact]
    public async Task GravarELerDocumentoDeUsuario_ComIdIgualAoUid()
    {
        var db = CriarDb();
        var uid = "uid-integracao-" + Guid.NewGuid().ToString("N");
        var docRef = db.Collection("usuarios").Document(uid);

        await docRef.SetAsync(new Dictionary<string, object>
        {
            ["uid"] = uid,
            ["nome"] = "Ana Teste",
            ["email"] = uid + "@exemplo.com",
            ["perfil"] = "Consumidor",
            ["ativo"] = true
        });

        var snapshot = await docRef.GetSnapshotAsync();

        Assert.True(snapshot.Exists);
        Assert.Equal(uid, snapshot.GetValue<string>("uid"));
        Assert.Equal("Consumidor", snapshot.GetValue<string>("perfil"));
    }

    [SkippableFact]
    public async Task DocumentoInexistente_RetornaNaoExiste()
    {
        var db = CriarDb();
        var uid = "uid-que-nao-existe-" + Guid.NewGuid().ToString("N");

        var snapshot = await db.Collection("usuarios").Document(uid).GetSnapshotAsync();

        Assert.False(snapshot.Exists);
    }
}