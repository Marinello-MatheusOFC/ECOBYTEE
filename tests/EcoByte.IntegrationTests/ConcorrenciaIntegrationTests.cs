using EcoByte.IntegrationTests;
using EcoByte.Web.Enums;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;

public class ConcorrenciaIntegrationTests
{
    static ConcorrenciaIntegrationTests()
    {
        AmbienteEmulador.ConfigurarAmbienteSeguro();
    }

    private static ServiceProvider Servicos => ServicosIntegracao.Provider();

    private static string EmailUnico(string prefixo)
        => $"{prefixo}-{Guid.NewGuid():N}@exemplo.com";

    private static async Task<Produto> CriarProdutoUnicoAsync(int quantidade)
    {
        var (uidParceiro, idEstabelecimento) =
            await CenarioIntegracao.ParceiroComEstabelecimentoAsync(Servicos, EmailUnico("parc"));

        var repositorio = Servicos.GetRequiredService<IProdutoRepository>();
        var produto = new Produto
        {
            Nome = "item disputado " + Guid.NewGuid().ToString("N")[..6],
            Descricao = "Produto para teste de concorrencia",
            PrecoOriginalCentavos = 1000,
            PrecoPromocionalCentavos = 400,
            QuantidadeDisponivel = quantidade,
            DataLimite = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(3)),
            Categoria = CategoriaProduto.Mercearia.ToFirestoreString(),
            Status = StatusProduto.Disponivel.ToFirestoreString(),
            EhVegano = false,
            EhSemGluten = false,
            EhSemLactose = false,
            EstabelecimentoId = idEstabelecimento,
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        await repositorio.CriarAsync(produto);
        return produto;
    }

    [SkippableFact]
    public async Task Concorrencia_EstoqueUm_DoisConsumidores_ApenasUmaSolicitacao()
    {
        EmuladorGuarda.Validar();

        var produto = await CriarProdutoUnicoAsync(1);
        var (uidConsumidorA, _) = await CenarioIntegracao.RegistrarConsumidorAsync(Servicos, EmailUnico("cons-a"));
        var (uidConsumidorB, _) = await CenarioIntegracao.RegistrarConsumidorAsync(Servicos, EmailUnico("cons-b"));

        var solicitacoes = Servicos.GetRequiredService<ISolicitacaoService>();

        async Task<Tentativa> TentarAsync(string uid)
        {
            try
            {
                await solicitacoes.CriarSolicitacaoAsync(produto.Id!, uid, 1);
                return Tentativa.Sucesso;
            }
            catch (InvalidOperationException)
            {
                return Tentativa.EstoqueEsgotado;
            }
            catch (Exception)
            {
                return Tentativa.FalhaInfraestrutura;
            }
        }

        var resultado = await Task.WhenAll(
            TentarAsync(uidConsumidorA),
            TentarAsync(uidConsumidorB));

        Assert.Equal(1, resultado.Count(r => r == Tentativa.Sucesso));
        Assert.Equal(1, resultado.Count(r => r == Tentativa.EstoqueEsgotado));
        Assert.Equal(0, resultado.Count(r => r == Tentativa.FalhaInfraestrutura));

        var produtos = Servicos.GetRequiredService<IProdutoRepository>();
        var produtoAtual = (await produtos.ObterPorIdAsync(produto.Id!))!;

        Assert.Equal(0, produtoAtual.QuantidadeDisponivel);
        Assert.Equal(StatusProduto.Esgotado.ToFirestoreString(), produtoAtual.Status);

        var db = Servicos.GetRequiredService<FirestoreDb>();
        var solicitacoesDoProduto = await db.Collection("solicitacoes")
            .WhereEqualTo("produtoId", produto.Id!)
            .GetSnapshotAsync();
        Assert.Single(solicitacoesDoProduto.Documents);

        var produtosNegativos = await db.Collection("produtos")
            .WhereLessThan("quantidadeDisponivel", 0)
            .GetSnapshotAsync();
        Assert.Empty(produtosNegativos.Documents);
    }

    private enum Tentativa
    {
        Sucesso,
        EstoqueEsgotado,
        FalhaInfraestrutura
    }
}