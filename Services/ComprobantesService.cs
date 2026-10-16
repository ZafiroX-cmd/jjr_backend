using JJR.Api.DTOs;
using JJR.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Services
{
    public class ComprobantesService : IComprobantesService
    {
        private readonly JjrDbContext _context;

        public ComprobantesService(JjrDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ComprobanteResponseDto>> ListarComprobantesAsync(ComprobanteFiltroDto filtro)
        {
            var query = _context.ComprobantesPagos.AsQueryable();

            // 1. Filtro por tipo de documento (BOLETA, FACTURA, etc.)
            if (!string.IsNullOrWhiteSpace(filtro.TipoComprobante))
            {
                var tipoNorm = filtro.TipoComprobante.Trim().ToUpper();
                query = query.Where(c => c.Documento.ToUpper() == tipoNorm);
            }

            // 2. Filtro por rango de fechas (DateOnly)
            if (filtro.FechaInicio.HasValue)
            {
                var fechaInicio = DateOnly.FromDateTime(filtro.FechaInicio.Value);
                query = query.Where(c => c.FechaDeEmision >= fechaInicio);
            }

            if (filtro.FechaFin.HasValue)
            {
                var fechaFin = DateOnly.FromDateTime(filtro.FechaFin.Value);
                query = query.Where(c => c.FechaDeEmision <= fechaFin);
            }

            // 3. Búsqueda por Serie, Correlativo/Número o Cliente
            if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
            {
                var busqueda = filtro.Busqueda.Trim().ToLower();
                query = query.Where(c =>
                    c.Serie.ToLower().Contains(busqueda) ||
                    c.Numero.ToString().Contains(busqueda) ||
                    c.ClienteNumeroDeDocumento.Contains(busqueda) ||
                    c.ClienteDenominacion.ToLower().Contains(busqueda)
                );
            }

            // Proyección exacta hacia ComprobanteResponseDto
            return await query
                .OrderByDescending(c => c.FechaDeEmision)
                .ThenByDescending(c => c.HoraDeEmision)
                .Select(c => new ComprobanteResponseDto
                {
                    // Se mapea la propiedad Guid según los DTOs o se expone el ID
                    Serie = c.Serie,
                    Correlativo = c.Numero.ToString("D8"),
                    TipoComprobante = c.Documento,
                    FechaEmision = c.FechaDeEmision.ToDateTime(c.HoraDeEmision ?? TimeOnly.MinValue),
                    ClienteDocumento = c.ClienteNumeroDeDocumento,
                    ClienteNombres = c.ClienteDenominacion,
                    Subtotal = c.MontoGravado,
                    Igv = c.MontoIgv,
                    Total = c.Total,
                    EstadoSunat = c.EstadoSunat,
                    MensajeSunat = c.RespuestaSunat
                })
                .ToListAsync();
        }

        public async Task<ComprobanteResponseDto?> ObtenerPorIdAsync(int id)
        {
            // NOTA: Como la PK en tu modelo es Guid (ComprobanteId), adaptamos la búsqueda
            // Si manejas búsqueda por el entero 'Numero':
            var c = await _context.ComprobantesPagos
                .FirstOrDefaultAsync(x => x.Numero == id);

            if (c == null) return null;

            return new ComprobanteResponseDto
            {
                Serie = c.Serie,
                Correlativo = c.Numero.ToString("D8"),
                TipoComprobante = c.Documento,
                FechaEmision = c.FechaDeEmision.ToDateTime(c.HoraDeEmision ?? TimeOnly.MinValue),
                ClienteDocumento = c.ClienteNumeroDeDocumento,
                ClienteNombres = c.ClienteDenominacion,
                Subtotal = c.MontoGravado,
                Igv = c.MontoIgv,
                Total = c.Total,
                EstadoSunat = c.EstadoSunat,
                MensajeSunat = c.RespuestaSunat
            };
        }

        public async Task<ComprobanteResponseDto> ConsultarEstadoSunatAsync(int id)
        {
            var comprobante = await ObtenerPorIdAsync(id);
            if (comprobante == null)
            {
                throw new KeyNotFoundException($"No se encontró el comprobante con número {id}");
            }

            return comprobante;
        }

        public async Task<bool> AnularComprobanteAsync(int id, string motivo)
        {
            var comprobante = await _context.ComprobantesPagos
                .FirstOrDefaultAsync(x => x.Numero == id);

            if (comprobante == null) return false;

            comprobante.EstadoSunat = "ANULADO";
            comprobante.Observacion = string.IsNullOrEmpty(comprobante.Observacion)
                ? $"Anulado: {motivo}"
                : $"{comprobante.Observacion} | Anulado: {motivo}";

            _context.ComprobantesPagos.Update(comprobante);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ArchivoComprobanteDto?> ObtenerArchivoAsync(int id, string formato)
        {
            // Búsqueda del comprobante por su número correlativo
            var comprobante = await _context.ComprobantesPagos
                .FirstOrDefaultAsync(x => x.Numero == id);

            if (comprobante == null) return null;

            string? rutaOUrl = formato.ToLower() switch
            {
                "pdf" => comprobante.PdfUrl,
                "xml" => comprobante.XmlUrl,
                _ => null
            };

            if (string.IsNullOrEmpty(rutaOUrl))
            {
                return null;
            }

            // 1. Si la URL es remota (http/https), la descargamos o la servimos
            if (rutaOUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rutaOUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using var httpClient = new HttpClient();
                var bytesRemotos = await httpClient.GetByteArrayAsync(rutaOUrl);
                return new ArchivoComprobanteDto
                {
                    NombreArchivo = $"{comprobante.Serie}-{comprobante.Numero:D8}.{formato.ToLower()}",
                    ContenidoBytes = bytesRemotos,
                    ContentType = formato.ToLower() == "pdf" ? "application/pdf" : "application/xml"
                };
            }

            // 2. Si la ruta es local dentro del servidor/proyecto
            string limpiaRuta = rutaOUrl.TrimStart('/', '\\');
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", limpiaRuta);

            if (!File.Exists(filePath))
            {
                // Ruta alternativa si los guardas directo por Serie-Correlativo
                string fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "vouchers", $"{comprobante.Serie}-{comprobante.Numero:D8}.{formato.ToLower()}");
                if (File.Exists(fallbackPath))
                {
                    filePath = fallbackPath;
                }
                else
                {
                    return null; // Archivo no encontrado localmente
                }
            }

            var bytes = await File.ReadAllBytesAsync(filePath);
            return new ArchivoComprobanteDto
            {
                NombreArchivo = Path.GetFileName(filePath),
                ContenidoBytes = bytes,
                ContentType = formato.ToLower() == "pdf" ? "application/pdf" : "application/xml"
            };
        }
    }
}