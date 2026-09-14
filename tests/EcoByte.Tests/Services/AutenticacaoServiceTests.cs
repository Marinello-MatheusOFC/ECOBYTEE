using EcoByte.Web.Enums;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.Models.Auth;
using EcoByte.Web.Services;
using Moq;

namespace EcoByte.Tests.Services;

public class AutenticacaoServiceTests
{
    private const string Uid = "uid-123";
    private const string Email = "ana@exemplo.com";
    private const string IdToken = "id-token";

    private readonly Mock<IAuthGateway> _authGateway;
    private readonly Mock<IFirebaseTokenValidator> _tokenValidator;
    private readonly Mock<IUsuarioService> _usuarioService;
    private readonly Mock<IEstabelecimentoService> _estabelecimentoService;
    private readonly AutenticacaoService _service;

    public AutenticacaoServiceTests()
    {
        _authGateway = new Mock<IAuthGateway>();
        _tokenValidator = new Mock<IFirebaseTokenValidator>();
        _usuarioService = new Mock<IUsuarioService>();
        _estabelecimentoService = new Mock<IEstabelecimentoService>();
        _service = new AutenticacaoService(
            _authGateway.Object,
            _tokenValidator.Object,
            _usuarioService.Object,
            _estabelecimentoService.Object);
    }

    [Fact]
    public async Task Login_CredenciaisInvalidas_RetornaFalhaComErroDoGateway()
    {
        _authGateway
            .Setup(g => g.AutenticarComSenhaAsync(Email, "senha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoCredenciais.Falha("EMAIL_NOT_FOUND"));

        var resultado = await _service.LoginAsync(Email, "senha");

        Assert.False(resultado.Sucesso);
        Assert.Equal("EMAIL_NOT_FOUND", resultado.Erro);
    }

    [Fact]
    public async Task Login_TokenInvalido_RetornaFalha()
    {
        _authGateway
            .Setup(g => g.AutenticarComSenhaAsync(Email, "senha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoCredenciais.SucessoToken(IdToken, Email));
        _tokenValidator
            .Setup(v => v.ValidarAsync(IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidacaoTokenResultado.Falha("Token invalido."));

        var resultado = await _service.LoginAsync(Email, "senha");

        Assert.False(resultado.Sucesso);
        Assert.Equal("Token invalido.", resultado.Erro);
    }

    [Fact]
    public async Task Login_UsuarioDesativado_RetornaFalha()
    {
        _authGateway
            .Setup(g => g.AutenticarComSenhaAsync(Email, "senha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoCredenciais.SucessoToken(IdToken, Email));
        _tokenValidator
            .Setup(v => v.ValidarAsync(IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidacaoTokenResultado.Sucesso(Uid, Email, true));
        _usuarioService
            .Setup(s => s.ObterPorUidAsync(Uid))
            .ReturnsAsync(new Usuario { Uid = Uid, Ativo = false });

        var resultado = await _service.LoginAsync(Email, "senha");

        Assert.False(resultado.Sucesso);
        Assert.Equal("Conta desativada.", resultado.Erro);
    }

    [Fact]
    public async Task Login_UsuarioNovoSemPerfil_RetornaSucessoSemEstabelecimento()
    {
        _authGateway
            .Setup(g => g.AutenticarComSenhaAsync(Email, "senha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoCredenciais.SucessoToken(IdToken, Email));
        _tokenValidator
            .Setup(v => v.ValidarAsync(IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidacaoTokenResultado.Sucesso(Uid, Email, true));
        _usuarioService
            .Setup(s => s.ObterPorUidAsync(Uid))
            .ReturnsAsync((Usuario?)null);

        var resultado = await _service.LoginAsync(Email, "senha");

        Assert.True(resultado.Sucesso);
        Assert.Equal(Uid, resultado.Uid);
        Assert.False(resultado.PossuiPerfil);
        Assert.Null(resultado.EstabelecimentoId);
    }

    [Fact]
    public async Task Login_ParceiroComEstabelecimento_RetornaEstabelecimentoId()
    {
        _authGateway
            .Setup(g => g.AutenticarComSenhaAsync(Email, "senha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoCredenciais.SucessoToken(IdToken, Email));
        _tokenValidator
            .Setup(v => v.ValidarAsync(IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidacaoTokenResultado.Sucesso(Uid, Email, true));

        var parceiro = new Usuario
        {
            Uid = Uid,
            Nome = "Ana",
            Email = Email,
            Perfil = PerfilUsuario.Parceiro.ToString(),
            Ativo = true
        };
        _usuarioService
            .Setup(s => s.ObterPorUidAsync(Uid))
            .ReturnsAsync(parceiro);
        _estabelecimentoService
            .Setup(s => s.ObterPorUsuarioAsync(Uid))
            .ReturnsAsync(new Estabelecimento { Id = "estab1" });

        var resultado = await _service.LoginAsync(Email, "senha");

        Assert.True(resultado.Sucesso);
        Assert.True(resultado.PossuiPerfil);
        Assert.Equal("estab1", resultado.EstabelecimentoId);
    }

    [Fact]
    public async Task Registrar_GatewayFalha_RetornaFalha()
    {
        _authGateway
            .Setup(g => g.RegistrarComSenhaAsync(Email, "senha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoCredenciais.Falha("EMAIL_EXISTS"));

        var resultado = await _service.RegistrarAsync("Ana", Email, "senha");

        Assert.False(resultado.Sucesso);
        Assert.Equal("EMAIL_EXISTS", resultado.Erro);
    }

    [Fact]
    public async Task Registrar_Sucesso_CriaUsuarioPadrao()
    {
        _authGateway
            .Setup(g => g.RegistrarComSenhaAsync(Email, "senha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoCredenciais.SucessoToken(IdToken, Email));
        _tokenValidator
            .Setup(v => v.ValidarAsync(IdToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidacaoTokenResultado.Sucesso(Uid, Email, false));

        var novo = new Usuario
        {
            Uid = Uid,
            Nome = "Ana",
            Email = Email,
            Perfil = PerfilUsuario.Consumidor.ToString(),
            Ativo = true
        };
        _usuarioService
            .Setup(s => s.ObouCriarAsync(Uid, "Ana", Email))
            .ReturnsAsync(novo);

        var resultado = await _service.RegistrarAsync("Ana", Email, "senha");

        Assert.True(resultado.Sucesso);
        Assert.Equal(Uid, resultado.Uid);
        Assert.Equal(PerfilUsuario.Consumidor.ToString(), resultado.Perfil);
        Assert.True(resultado.PossuiPerfil);
        _usuarioService.Verify(s => s.ObouCriarAsync(Uid, "Ana", Email), Times.Once);
    }
}