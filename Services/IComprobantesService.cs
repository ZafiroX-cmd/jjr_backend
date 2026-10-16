using JJR.Api.DTOs;

namespace JJR.Api.Services
{
    public interface IComprobantesService
    {
        Task<IEnumerable<ComprobanteResponseDto>> ListarComprobantesAsync(ComprobanteFiltroDto filtro);
        Task<ComprobanteResponseDto?> ObtenerPorIdAsync(int id);
        Task<ComprobanteResponseDto> ConsultarEstadoSunatAsync(int id);
        Task<bool> AnularComprobanteAsync(int id, string motivo);
        Task<ArchivoComprobanteDto?> ObtenerArchivoAsync(int id, string formato);
    }
}