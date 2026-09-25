using JJR.Api.Dtos;

namespace JJR.Api.Services
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioResponseDto>> ObtenerTodosAsync();
        Task<(bool Exito, string Mensaje, string? UsuarioId)> CrearAsync(CrearUsuarioDto dto);
        Task<(bool Exito, string Mensaje)> ActualizarAsync(string usuarioId, ActualizarUsuarioDto dto);
    }
}