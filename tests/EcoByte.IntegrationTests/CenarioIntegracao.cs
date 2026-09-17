using EcoByte.Web.Enums;
using EcoByte.Web.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class CenarioIntegracao
{
    public static async Task<(string Uid, string Email)> RegistrarConsumidorAsync(
        ServiceProvider servicos, string email)
    {
        var autenticacao = servicos.GetRequiredService<IAutenticacaoService>();
        var registro = await autenticacao.RegistrarAsync("Consumidor Teste", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), registro.Perfil);
        return (registro.Uid!, email);
    }

    public static async Task<(string Uid, string EstabelecimentoId)> ParceiroComEstabelecimentoAsync(
        ServiceProvider servicos, string email)
    {
        var autenticacao = servicos.GetRequiredService<IAutenticacaoService>();
        var usuarios = servicos.GetRequiredService<IUsuarioService>();
        var estabelecimentos = servicos.GetRequiredService<IEstabelecimentoService>();

        var registro = await autenticacao.RegistrarAsync("Parceiro Teste", email, "Senha123!");
        Assert.True(registro.Sucesso, registro.Erro);

        await usuarios.DefinirPerfilAsync(
            registro.Uid!, email, "Parceiro Teste", PerfilUsuario.Parceiro.ToString());

        var idEstabelecimento = await estabelecimentos.CriarAsync(
            registro.Uid!, "Comercio Teste", "(11) 97777-0000", email, "Comercio", "Rua B, 200");

        return (registro.Uid!, idEstabelecimento);
    }
}