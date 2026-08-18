namespace EcoByte.Web.Services;

using EcoByte.Web.Enums;
using EcoByte.Web.Exceptions;
using EcoByte.Web.Extensions;
using EcoByte.Web.Interfaces;
using EcoByte.Web.Models;
using Google.Cloud.Firestore;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repository;

    public UsuarioService(IUsuarioRepository repository) => _repository = repository;

    public async Task<Usuario?> ObterPorUidAsync(string uid)
        => await _repository.ObterPorUidAsync(uid);

    public async Task<Usuario> ObouCriarAsync(string uid, string nome, string email)
    {
        var existente = await _repository.ObterPorUidAsync(uid);
        if (existente is not null) return existente;

        var usuario = new Usuario
        {
            Uid = uid,
            Nome = nome,
            Email = email,
            Perfil = PerfilUsuario.Consumidor.ToString(),
            Ativo = true,
            CriadoEm = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        return await _repository.CriarAsync(usuario);
    }

    public async Task AtualizarPerfilAsync(string uid, string nome)
    {
        var usuario = await _repository.ObterPorUidAsync(uid)
            ?? throw new KeyNotFoundException("Usuario nao encontrado.");

        usuario.Nome = nome;
        await _repository.AtualizarAsync(usuario);
    }

    public async Task<List<Usuario>> ObterTodosAsync(int pagina, int tamanhoPagina)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        return await _repository.ObterTodosAsync(tamanhoPagina, offset);
    }

    public async Task<int> ContarTodosAsync()
        => await _repository.ContarTodosAsync();
}
