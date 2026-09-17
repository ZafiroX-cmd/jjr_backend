using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JJR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PasajesController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public PasajesController(JjrDbContext context)
        {
            _context = context;
        }

        // GET: api/Pasajes/viaje/{viajeId}
        [HttpGet("viaje/{viajeId}")]
        public async Task<IActionResult> GetMapaCamioneta(Guid viajeId)
        {
            var viaje = await _context.Viajes
                .Include(v => v.Ruta)
                .Include(v => v.Vehiculo)
                .Include(v => v.Pasajes)
                    .ThenInclude(p => p.ClientePasajero)
                .Include(v => v.Pasajes)
                    .ThenInclude(p => p.DestinoParada)
                .FirstOrDefaultAsync(v => v.ViajeId == viajeId);

            if (viaje == null)
                return NotFound(new { mensaje = "La salida programada no existe." });

            var paradas = await _context.RutaParadas
                .Where(p => p.RutaId == viaje.RutaId && p.Activo)
                .OrderBy(p => p.Orden)
                .Select(p => new
                {
                    paradaId = p.ParadaId,
                    nombreParada = p.NombreParada,
                    orden = p.Orden,
                    esOficina = p.EsOficina
                })
                .ToListAsync();

            var asientos = Enumerable.Range(1, 4).Select(numAsiento =>
            {
                var pasaje = viaje.Pasajes.FirstOrDefault(p => p.NumeroAsiento == numAsiento && p.Estado != "Anulado");

                string ubicacionNombre = numAsiento switch
                {
                    1 => "Copiloto",
                    2 => "Atrás izquierda",
                    3 => "Atrás centro",
                    4 => "Atrás derecha",
                    _ => "Asiento"
                };

                return new
                {
                    numeroAsiento = numAsiento,
                    ubicacion = ubicacionNombre,
                    ocupado = pasaje != null,
                    estado = pasaje != null ? pasaje.Estado : "Disponible", // 'Reservado' o 'Emitido'
                    tipoOperacion = pasaje?.TipoOperacion,
                    precioTotal = pasaje?.Precio ?? 0,
                    montoAdelanto = pasaje?.MontoAdelanto ?? 0,
                    saldoPendiente = pasaje != null ? (pasaje.Precio - pasaje.MontoAdelanto) : 0,
                    medioPago = pasaje?.MedioPago,
                    comprobantePagoUrl = pasaje?.ComprobantePagoUrl,
                    comprobanteEmitido = pasaje?.ComprobanteEmitido ?? false,
                    pasajeroNombre = pasaje != null
                        ? (string.IsNullOrEmpty(pasaje.ClientePasajero.Nombres)
                            ? pasaje.ClientePasajero.RazonSocial
                            : $"{pasaje.ClientePasajero.Nombres} {pasaje.ClientePasajero.ApellidoPaterno}".Trim())
                        : null,
                    pasajeroDocumento = pasaje?.ClientePasajero.NumeroDocumento,
                    destinoNombre = pasaje?.DestinoParada?.NombreParada,
                    pasajeId = pasaje?.PasajeId
                };
            }).ToList();

            return Ok(new
            {
                viajeId = viaje.ViajeId,
                rutaNombre = viaje.Ruta.Origen + " - " + viaje.Ruta.Destino,
                placa = viaje.Vehiculo.Placa,
                precioBase = viaje.PrecioPasaje,
                fechaSalida = viaje.FechaSalida.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                paradas = paradas,
                asientos = asientos
            });
        }

        // POST: api/Pasajes
        [HttpPost]
        public async Task<IActionResult> VenderOPasaje([FromForm] VentaPasajeFormDto model)
        {
            var userOficinaClaim = User.FindFirst("OficinaId")?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (model.NumeroAsiento < 1 || model.NumeroAsiento > 4)
            {
                return BadRequest(new { mensaje = "El número de asiento debe estar entre 1 y 4." });
            }

            // 1. Validar disponibilidad del asiento
            var ocupado = await _context.Pasajes.AnyAsync(p =>
                p.ViajeId == model.ViajeId &&
                p.NumeroAsiento == model.NumeroAsiento &&
                p.Estado != "Anulado");

            if (ocupado)
            {
                return BadRequest(new { mensaje = $"El asiento #{model.NumeroAsiento} ya está ocupado o reservado." });
            }

            // 2. Validar secuencia de paradas del tramo
            var paradaOrigen = await _context.RutaParadas.FindAsync(model.OrigenParadaId);
            var paradaDestino = await _context.RutaParadas.FindAsync(model.DestinoParadaId);

            if (paradaOrigen == null || paradaDestino == null || paradaDestino.Orden <= paradaOrigen.Orden)
            {
                return BadRequest(new { mensaje = "La parada de bajada debe ser posterior a la de subida." });
            }

            // 3. Procesar Voucher para pagos digitales 
            string? voucherUrl = null;
            if (model.MedioPago != "EFECTIVO")
            {
                if (model.VoucherFile == null || model.VoucherFile.Length == 0)
                {
                    return BadRequest(new { mensaje = "Debe adjuntar la captura/comprobante para pagos digitales (Yape, Plin o Transferencia)." });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "vouchers");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.VoucherFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.VoucherFile.CopyToAsync(stream);
                }

                voucherUrl = $"/uploads/vouchers/{fileName}";
            }

            int? oficinaEmisionId = int.TryParse(userOficinaClaim, out int ofId) ? ofId : null;

            if (!Guid.TryParse(userIdClaim, out Guid usuarioId))
            {
                var username = User.Identity?.Name;
                var userObj = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username);
                if (userObj != null) usuarioId = userObj.UsuarioId;
            }

            // 4. Lógica de Cobro y Estado
            string tipoOp = model.TipoOperacion.ToUpper() == "RESERVA" ? "RESERVA" : "VENTA_DIRECTA";
            decimal cobrado = tipoOp == "RESERVA" ? model.MontoAdelanto : model.Precio;

            
            string estadoFinal = (tipoOp == "VENTA_DIRECTA" || cobrado >= model.Precio) ? "Emitido" : "Reservado";

            var nuevoPasaje = new Pasaje
            {
                PasajeId = Guid.NewGuid(),
                ViajeId = model.ViajeId,
                NumeroAsiento = model.NumeroAsiento,
                ClientePasajeroId = model.ClientePasajeroId,
                ClienteFacturacionId = model.ClienteFacturacionId ?? model.ClientePasajeroId,
                Precio = model.Precio,
                TipoOperacion = tipoOp,
                MontoAdelanto = cobrado,
                MedioPago = model.MedioPago,
                ComprobantePagoUrl = voucherUrl,
                NumeroOperacion = model.NumeroOperacion,
                Estado = estadoFinal,
                ComprobanteEmitido = false,
                CreadoPorUsuarioId = usuarioId,
                OficinaEmisionId = oficinaEmisionId,
                OrigenParadaId = model.OrigenParadaId,
                DestinoParadaId = model.DestinoParadaId,
                CreadoEn = DateTime.UtcNow
            };

            _context.Pasajes.Add(nuevoPasaje);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = tipoOp == "RESERVA" ? "Reserva registrada correctamente." : "Venta registrada exitosamente.",
                pasajeId = nuevoPasaje.PasajeId,
                estado = nuevoPasaje.Estado,
                puedeEmitirComprobante = cobrado >= model.Precio
            });
        }

        // PATCH: api/Pasajes/{id}/completar-pago
        [HttpPatch("{id}/completar-pago")]
        public async Task<IActionResult> CompletarPagoReserva(Guid id, [FromForm] LiquidarReservaDto model)
        {
            var pasaje = await _context.Pasajes.FindAsync(id);
            if (pasaje == null) return NotFound(new { mensaje = "El pasaje no existe." });

            if (pasaje.Estado == "Anulado") return BadRequest(new { mensaje = "No se puede liquidar un pasaje anulado." });

            decimal saldoPendiente = pasaje.Precio - pasaje.MontoAdelanto;
            if (model.MontoPago < saldoPendiente)
            {
                return BadRequest(new { mensaje = $"El saldo pendiente es S/ {saldoPendiente:F2}. El pago debe saldar la totalidad para poder emitir comprobante." });
            }

            if (model.MedioPago != "EFECTIVO" && (model.VoucherFile == null || model.VoucherFile.Length == 0))
            {
                return BadRequest(new { mensaje = "Adjunte la imagen del comprobante para verificar el pago digital." });
            }

            if (model.VoucherFile != null && model.VoucherFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "vouchers");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.VoucherFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.VoucherFile.CopyToAsync(stream);
                }
                pasaje.ComprobantePagoUrl = $"/uploads/vouchers/{fileName}";
            }

            pasaje.MontoAdelanto = pasaje.Precio;
            pasaje.Estado = "Emitido";

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Reserva saldada exitosamente. Ya puede emitir la Boleta/Factura o Nota de Venta." });
        }

        // DELETE: api/Pasajes/{id}/cancelar
        [HttpDelete("{id}/cancelar")]
        public async Task<IActionResult> CancelarPasaje(Guid id)
        {
            var pasaje = await _context.Pasajes.FindAsync(id);
            if (pasaje == null) return NotFound(new { mensaje = "El pasaje o reserva no existe." });

            pasaje.Estado = "Anulado";
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Pasaje/Reserva anulado. El asiento ha sido liberado." });
        }
    }

    #region --- DTOs ---

    public class VentaPasajeFormDto
    {
        public Guid ViajeId { get; set; }
        public int NumeroAsiento { get; set; }
        public Guid ClientePasajeroId { get; set; }
        public Guid? ClienteFacturacionId { get; set; }
        public int OrigenParadaId { get; set; }
        public int DestinoParadaId { get; set; }
        public decimal Precio { get; set; }
        public string TipoOperacion { get; set; } = "VENTA_DIRECTA"; 
        public decimal MontoAdelanto { get; set; } = 0.00m;
        public string MedioPago { get; set; } = "EFECTIVO"; 
        public string? NumeroOperacion { get; set; }
        public IFormFile? VoucherFile { get; set; }
    }

    public class LiquidarReservaDto
    {
        public decimal MontoPago { get; set; }
        public string MedioPago { get; set; } = "EFECTIVO";
        public IFormFile? VoucherFile { get; set; }
    }

    #endregion
}