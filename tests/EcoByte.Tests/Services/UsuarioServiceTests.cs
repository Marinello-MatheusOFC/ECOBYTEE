using EcoByte.Web.Enums;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.Services;
using Moq;

namespace EcoByte.Tests.Services;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _repository;

    public UsuarioServiceTests()
    {
        _repository = new Mock<IUsuarioRepository>();
    }

    [Fact]
    public async Task ObouCriar_SemUsuarioExistente_CriaComPerfilConsumidor()
    {
        _repository
            .Setup(r => r.ObterPorUidAsync("uid1"))
            .ReturnsAsync((Usuario?)null);
        _repository
            .Setup(r => r.CriarAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario usuario) => usuario);

        var service = new UsuarioService(_repository.Object);
        var usuario = await service.ObouCriarAsync("uid1", "Ana", "ana@exemplo.com");

        Assert.Equal(PerfilUsuario.Consumidor.ToString(), usuario.Perfil);
        Assert.True(usuario.Ativo);
        Assert.Equal("uid1", usuario.Uid);
    }

    [Fact]
    public async Task DefinirPerfil_PerfilAdministrador_LancaExcecao()
    {
        var service = new UsuarioService(_repository.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.DefinirPerfilAsync(
                "uid1", "ana@exemplo.com", "Ana", PerfilUsuario.Administrador.ToString()));
    }

    [Fact]
    public async Task DefinirPerfil_UsuarioExistente_AtualizaPerfil()
    {
        var existente = new Usuario
        {
            Uid = "uid1",
            Nome = "Ana",
            Email = "ana@exemplo.com",
            Perfil = PerfilUsuario.Consumidor.ToString(),
            Ativo = true
        };
        _repository
            .Setup(r => r.ObterPorUidAsync("uid1"))
            .ReturnsAsync(existente);

        var service = new UsuarioService(_repository.Object);
        await service.DefinirPerfilAsync("uid1", "ana@exemplo.com", "Ana", PerfilUsuario.Parceiro.ToString());

        _repository.Verify(
            r => r.AtualizarAsync(It.Is<Usuario>(u => u.Perfil == PerfilUsuario.Parceiro.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task DefinirPerfil_UsuarioNaoExistente_CriaNovo()
    {
        _repository
            .Setup(r => r.ObterPorUidAsync("uid1"))
            .ReturnsAsync((Usuario?)null);
        _repository
            .Setup(r => r.CriarAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario usuario) => usuario);

        var service = new UsuarioService(_repository.Object);
        await service.DefinirPerfilAsync("uid1", "ana@exemplo.com", "Ana", PerfilUsuario.Ong.ToString());

        _repository.Verify(r => r.CriarAsync(It.Is<Usuario>(u => u.Perfil == PerfilUsuario.Ong.ToString())), Times.Once);
    }

    [Fact]
    public async Task AtualizarPerfil_UsuarioNaoExiste_LancaKeyNotFound()
    {
        _repository
            .Setup(r => r.ObterPorUidAsync("uid1"))
            .ReturnsAsync((Usuario?)null);

        var service = new UsuarioService(_repository.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AtualizarPerfilAsync("uid1", "Novo Nome"));
    }
}