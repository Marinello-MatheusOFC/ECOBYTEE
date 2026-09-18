namespace EcoByte.Web.Models;

public enum SituacaoUsuarioLegado
{
    EmDia,
    Migrar,
    Ignorado
}

public record ItemRelatorioMigracaoUsuarioLegado(
    string DocumentoId,
    string? UidEncontrado,
    SituacaoUsuarioLegado Situacao,
    string Descricao);

public class RelatorioMigracaoUsuariosLegados
{
    public bool SomenteSimulacao { get; set; } = true;
    public DateTime GeradoEm { get; set; }
    public List<ItemRelatorioMigracaoUsuarioLegado> Itens { get; set; } = new();

    public int TotalEmDia => Itens.Count(i => i.Situacao == SituacaoUsuarioLegado.EmDia);
    public int TotalParaMigrar => Itens.Count(i => i.Situacao == SituacaoUsuarioLegado.Migrar);
    public int TotalIgnorados => Itens.Count(i => i.Situacao == SituacaoUsuarioLegado.Ignorado);
    public int TotalAnalisados => Itens.Count;
}