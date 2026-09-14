namespace EcoByte.Web.Services;

using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using EcoByte.Web.Models.Auth;

public class AutenticacaoService : IAutenticacaoService
{
    private readonly IAuthGateway _authGateway;
    private readonly IFirebaseTokenValidator _tokenValidator;
    private readonly IUsuarioService _usuarioService;
    private readonly IEstabelecimentoService _estabelecimentoService;

    public AutenticacaoService(
        IAuthGateway authGateway,
        IFirebaseTokenValidator tokenValidator,
        IUsuarioService usuarioService,
        IEstabelecimentoService estabelecimentoService)
    {
        _authGateway = authGateway;
        _tokenValidator = tokenValidator;
        _usuarioService = usuarioService;
        _estabelecimentoService = estabelecimentoService;
    }

    public async Task<ResultadoAutenticacao> LoginAsync(
        string email, string senha, CancellationToken cancellationToken = default)
    {
        var credenciais = await _authGateway.AutenticarComSenhaAsync(email, senha, cancellationToken);
        if (!credenciais.Sucesso)
            return ResultadoAutenticacao.Falha(credenciais.Erro ?? "Credenciais invalidas.");

        return await ValidarEConstruirAsync(credenciais, cancellationToken);
    }

    public async Task<ResultadoAutenticacao> RegistrarAsync(
        string nome, string email, string senha, CancellationToken cancellationToken = default)
    {
        var credenciais = await _authGateway.RegistrarComSenhaAsync(email, senha, cancellationToken);
        if (!credenciais.Sucesso)
            return ResultadoAutenticacao.Falha(credenciais.Erro ?? "Nao foi possivel criar a conta.");

        var resultado = await ValidarEConstruirAsync(credenciais, cancellationToken);
        if (!resultado.Sucesso)
            return resultado;

        var nomeFinal = string.IsNullOrWhiteSpace(nome) ? resultado.Email ?? "Usuario" : nome;

        var usuario = await _usuarioService.ObouCriarAsync(
            resultado.Uid!, nomeFinal, resultado.Email ?? credenciais.Email ?? string.Empty);

        return ResultadoAutenticacao.SucessoAutenticacao(
            resultado.Uid!,
            resultado.Email ?? credenciais.Email ?? string.Empty,
            resultado.EmailVerificado,
            usuario.Nome,
            usuario.Perfil,
            possuiPerfil: true);
    }

    private async Task<ResultadoAutenticacao> ValidarEConstruirAsync(
        ResultadoCredenciais credenciais, CancellationToken cancellationToken)
    {
        var validacao = await _tokenValidator.ValidarAsync(credenciais.IdToken, cancellationToken);
        if (!validacao.Valido || string.IsNullOrWhiteSpace(validacao.Uid))
            return ResultadoAutenticacao.Falha(validacao.Erro ?? "Token invalido.");

        var usuario = await _usuarioService.ObterPorUidAsync(validacao.Uid);

        if (usuario is not null && !usuario.Ativo)
            return ResultadoAutenticacao.Falha("Conta desativada.");

        var estabelecimentoId = await ObterEstabelecimentoId(usuario, cancellationToken);

        return ResultadoAutenticacao.SucessoAutenticacao(
            validacao.Uid,
            validacao.Email ?? credenciais.Email ?? string.Empty,
            validacao.EmailVerificado,
            usuario?.Nome,
            usuario?.Perfil,
            possuiPerfil: usuario is not null,
            estabelecimentoId: estabelecimentoId);
    }

    private async Task<string?> ObterEstabelecimentoId(
        Usuario? usuario, CancellationToken cancellationToken)
    {
        if (usuario?.Perfil != Enums.PerfilUsuario.Parceiro.ToString())
            return null;

        var estabelecimento = await _estabelecimentoService.ObterPorUsuarioAsync(usuario.Uid);

        return estabelecimento?.Id;
    }
}