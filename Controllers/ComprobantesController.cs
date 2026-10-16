using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JJR.Api.DTOs;
using JJR.Api.Services;

namespace JJR.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ComprobantesController : ControllerBase
    {
        private readonly IComprobantesService _comprobantesService;

        public ComprobantesController(IComprobantesService comprobantesService)
        {
            _comprobantesService = comprobantesService;
        }

        /// <summary>
        /// Obtiene el listado de comprobantes filtrados por tipo, fechas o texto.
        /// GET: api/Comprobantes
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerComprobantes(
            [FromQuery] string? tipo,
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin,
            [FromQuery] string? busqueda)
        {
            var filtro = new ComprobanteFiltroDto
            {
                TipoComprobante = tipo,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Busqueda = busqueda
            };

            var comprobantes = await _comprobantesService.ListarComprobantesAsync(filtro);
            return Ok(comprobantes);
        }

        /// <summary>
        /// Obtiene el detalle de un comprobante específico por su ID.
        /// GET: api/Comprobantes/{id}
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var comprobante = await _comprobantesService.ObtenerPorIdAsync(id);
            if (comprobante == null)
            {
                return NotFound(new { mensaje = "Comprobante no encontrado." });
            }

            return Ok(comprobante);
        }

        /// <summary>
        /// Consulta el estado de validez y recepción ante SUNAT / Decolecta.
        /// POST: api/Comprobantes/{id}/consultar-sunat
        /// </summary>
        [HttpPost("{id:int}/consultar-sunat")]
        public async Task<IActionResult> ConsultarEstadoSunat(int id)
        {
            try
            {
                var resultado = await _comprobantesService.ConsultarEstadoSunatAsync(id);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Solicita la anulación o emisión de nota de crédito para un comprobante.
        /// POST: api/Comprobantes/{id}/anular
        /// </summary>
        [HttpPost("{id:int}/anular")]
        public async Task<IActionResult> AnularComprobante(int id, [FromBody] AnularComprobanteRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Motivo))
            {
                return BadRequest(new { mensaje = "Debe especificar un motivo de anulación." });
            }

            try
            {
                var exito = await _comprobantesService.AnularComprobanteAsync(id, request.Motivo);
                if (!exito) return BadRequest(new { mensaje = "No se pudo anular el comprobante especificado." });

                return Ok(new { mensaje = "Comprobante anulado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Descarga la representación impresa PDF, XML o CDR del comprobante.
        /// GET: api/Comprobantes/{id}/descargar/{formato}
        /// </summary>
        [HttpGet("{id:int}/descargar/{formato}")]
        public async Task<IActionResult> DescargarArchivo(int id, string formato)
        {
            var archivo = await _comprobantesService.ObtenerArchivoAsync(id, formato);
            if (archivo == null)
            {
                return NotFound(new { mensaje = "El archivo solicitado no se encuentra disponible." });
            }

            return File(archivo.ContenidoBytes, archivo.ContentType, archivo.NombreArchivo);
        }
    }
}