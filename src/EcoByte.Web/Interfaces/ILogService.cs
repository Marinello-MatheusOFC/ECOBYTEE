namespace EcoByte.Web.Interfaces;

using EcoByte.Web.Models;

public interface ILogService
{
    Task RegistrarAsync(string usuarioId, string acao, string entidade, string? entidadeId, string resultado);
}
