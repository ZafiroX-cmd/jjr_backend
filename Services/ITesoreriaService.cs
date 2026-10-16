using JJR.Api.DTOs;

namespace JJR.Api.Services
{
    public interface ITesoreriaService
    {
        Task<MovimientoCajaResponseDto> RegistrarMovimientoAsync(RegistrarMovimientoRequestDto dto, Guid usuarioId);
        Task<IEnumerable<MovimientoCajaResponseDto>> ListarMovimientosAsync(MovimientoFiltroDto filtro);
        Task<MovimientoCajaResponseDto?> ObtenerPorIdAsync(Guid movimientoId);
    }
}