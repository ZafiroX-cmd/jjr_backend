using JJR.Api.Dtos;
using JJR.Api.Models;
using JJR.Api.Services;
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

            var pasajeIds = viaje.Pasajes.Where(p => p.Estado != "Anulado").Select(p => p.PasajeId).ToList();

            var comprobantes = await _context.ComprobantesPagos
                .Where(c => pasajeIds.Contains(c.PasajeId.Value))
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

                string? numeroComprobanteFormat = null;
                string? tipoComprobanteFormat = null;

                if (pasaje != null)
                {
                    var comprobante = comprobantes.FirstOrDefault(c => c.PasajeId == pasaje.PasajeId);
                    if (comprobante != null)
                    {
                        numeroComprobanteFormat = $"{comprobante.Serie} - {comprobante.Numero:D8}";

                        tipoComprobanteFormat = comprobante.Documento.ToLower() switch
                        {
                            "factura" => "FACTURA",
                            "boleta" => "BOLETA",
                            _ => "NOTA_VENTA"
                        };
                    }
                }

                return new
                {
                    numeroAsiento = numAsiento,
                    ubicacion = ubicacionNombre,
                    ocupado = pasaje != null,
                    estado = pasaje != null ? pasaje.Estado : "Disponible",
                    tipoOperacion = pasaje?.TipoOperacion,
                    precioTotal = pasaje?.Precio ?? 0,
                    montoAdelanto = pasaje?.MontoAdelanto ?? 0,
                    saldoPendiente = pasaje != null ? (pasaje.Precio - pasaje.MontoAdelanto) : 0,
                    medioPago = pasaje?.MedioPago,
                    numeroOperacion = pasaje?.NumeroOperacion,
                    comprobantePagoUrl = pasaje?.ComprobantePagoUrl,
                    comprobanteEmitido = pasaje?.ComprobanteEmitido ?? false,

                    numeroComprobante = numeroComprobanteFormat,
                    tipoComprobante = tipoComprobanteFormat,

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
        public async Task<IActionResult> VenderOPasaje(
            [FromForm] VentaPasajeFormDto model,
            [FromServices] ApiSunatService apiSunat)
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

            // 4. Lógica de Cobro y Estado del Pasaje
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

            // 5. Emisión de Comprobante Solo si es VENTA DIRECTA
            string? tipoCompUpper = model.TipoComprobante?.ToUpper();
            bool solicitaComprobante = tipoOp == "VENTA_DIRECTA" && !string.IsNullOrEmpty(tipoCompUpper);

            if (solicitaComprobante)
            {
                var cliente = await _context.Clientes.FindAsync(model.ClientePasajeroId);
                if (cliente == null)
                    return BadRequest(new { mensaje = "No se encontró el cliente registrado para emitir el comprobante." });

                var emitido = await GenerarComprobanteInternoOSunat(nuevoPasaje, cliente, tipoCompUpper!, oficinaEmisionId, usuarioId, apiSunat);
                if (!emitido.success)
                    return BadRequest(new { mensaje = emitido.error });
            }

            _context.Pasajes.Add(nuevoPasaje);

            // -----------------------------------------------------------------
            // 🚀 REGISTRO AUTOMÁTICO EN TESORERÍA / CAJA
            // -----------------------------------------------------------------
            if (cobrado > 0)
            {
                var cuentaEmpresa = await _context.CuentasEmpresas
                    .FirstOrDefaultAsync(c => c.Activo);

                if (cuentaEmpresa != null)
                {
                    var movimientoTesoreria = new MovimientosCaja
                    {
                        MovimientoId = Guid.NewGuid(),
                        CuentaId = cuentaEmpresa.CuentaId,
                        TipoMovimiento = "INGRESO",
                        Monto = cobrado,
                        TipoMovimiento = TipoMovimientoCaja.INGRESO,
                        Categoria = "VENTA_PASAJES",
                        Descripcion = $"Venta Pasaje Asiento #{nuevoPasaje.NumeroAsiento} - {model.MedioPago}",
                        UsuarioRegistroId = usuarioId,
                        FechaMovimiento = DateTime.UtcNow
                    };

                    _context.MovimientosCajas.Add(movimientoTesoreria);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = tipoOp == "RESERVA" ? "Reserva registrada correctamente." : "Venta registrada exitosamente.",
                pasajeId = nuevoPasaje.PasajeId,
                estado = nuevoPasaje.Estado,
                comprobanteEmitido = nuevoPasaje.ComprobanteEmitido,
                puedeEmitirComprobante = cobrado >= model.Precio
            });
        }

        // PATCH: api/Pasajes/{id}/completar-pago
        [HttpPatch("{id}/completar-pago")]
        public async Task<IActionResult> CompletarPagoReserva(
            Guid id,
            [FromForm] LiquidarReservaDto model,
            [FromServices] ApiSunatService apiSunat)
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

            var userOficinaClaim = User.FindFirst("OficinaId")?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? oficinaEmisionId = int.TryParse(userOficinaClaim, out int ofId) ? ofId : pasaje.OficinaEmisionId;

            if (!Guid.TryParse(userIdClaim, out Guid usuarioId))
            {
                usuarioId = pasaje.CreadoPorUsuarioId;
            }

            // EMISIÓN DEL COMPROBANTE AL LIQUIDAR LA RESERVA
            if (!string.IsNullOrEmpty(model.TipoComprobante))
            {
                var cliente = await _context.Clientes.FindAsync(pasaje.ClientePasajeroId);
                if (cliente == null)
                    return BadRequest(new { mensaje = "Cliente no encontrado para emitir comprobante." });

                var res = await GenerarComprobanteInternoOSunat(pasaje, cliente, model.TipoComprobante.ToUpper(), oficinaEmisionId, usuarioId, apiSunat);
                if (!res.success)
                {
                    return BadRequest(new { mensaje = res.error });
                }
            }

            // -----------------------------------------------------------------
            // 🚀 REGISTRO AUTOMÁTICO DE LIQUIDACIÓN DE RESERVA EN TESORERÍA
            // -----------------------------------------------------------------
            var cuentaEmpresaReserva = await _context.CuentasEmpresas
                .FirstOrDefaultAsync(c => c.Activo);

            if (cuentaEmpresaReserva != null)
            {
                var movimientoReservaTesoreria = new MovimientosCaja
                {
                    MovimientoId = Guid.NewGuid(),
                    CuentaId = cuentaEmpresaReserva.CuentaId,
                    TipoMovimiento = "INGRESO",
                    Monto = saldoPendiente,
                    TipoMovimiento = TipoMovimientoCaja.INGRESO,
                    Categoria = "VENTA_PASAJES",
                    Descripcion = $"Cancelación Saldo Reserva Asiento #{pasaje.NumeroAsiento} - {model.MedioPago}",
                    UsuarioRegistroId = usuarioId,
                    FechaMovimiento = DateTime.UtcNow
                };

                _context.MovimientosCajas.Add(movimientoReservaTesoreria);
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Reserva saldada exitosamente y comprobante generado." });
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

        #region --- MÉTODO AUXILIAR DE EMISIÓN DE COMPROBANTES ---

        private async Task<(bool success, string? error)> GenerarComprobanteInternoOSunat(
            Pasaje pasaje, Cliente cliente, string tipoCompUpper, int? oficinaEmisionId, Guid usuarioId, ApiSunatService apiSunat)
        {
            bool esFactura = tipoCompUpper == "FACTURA";
            bool esNotaVenta = tipoCompUpper == "NOTA_VENTA" || tipoCompUpper == "NOTA_DE_VENTA";
            string docTipo = esNotaVenta ? "nota_de_venta" : (esFactura ? "factura" : "boleta");

            if (esFactura && cliente.NumeroDocumento.Length != 11)
            {
                return (false, "Para emitir una Factura Electrónica se requiere un RUC válido de 11 dígitos.");
            }

            string sufijoOficina = oficinaEmisionId switch
            {
                1 => "001", // Trujillo
                2 => "002", // Chillia
                3 => "003", // Deliciana
                _ => "001"
            };

            string prefijoTipo = esNotaVenta ? "NV" : (esFactura ? "F" : "B");
            string serieBuscada = $"{prefijoTipo}{sufijoOficina}";

            var serieObj = await _context.SeriesComprobantes
                .FirstOrDefaultAsync(s => s.TipoDocumento.ToLower() == docTipo &&
                                          s.Serie == serieBuscada &&
                                          s.Activo);

            if (serieObj == null)
            {
                return (false, $"No existe una serie activa ({serieBuscada}) configurada para {docTipo} en esta oficina.");
            }

            int nuevoNumero = serieObj.UltimoNumero + 1;
            serieObj.UltimoNumero = nuevoNumero;

            decimal total = pasaje.Precio;
            decimal montoGravado = Math.Round(total / 1.18m, 2);
            decimal montoIgv = total - montoGravado;
            decimal valorUnitario6Dec = Math.Round(total / 1.18m, 6);

            DateTime ahora = DateTime.UtcNow.AddHours(-5); // UTC-5 (Perú)

            ApiSunatResponseDto sunatResponse = new() { success = true, message = "Nota de Venta emitida internamente" };

            if (!esNotaVenta)
            {
                var sunatPayload = new ApiSunatDocumentDto
                {
                    documento = docTipo,
                    serie = serieObj.Serie,
                    numero = nuevoNumero,
                    fecha_de_emision = ahora.ToString("yyyy-MM-dd"),
                    hora_de_emision = ahora.ToString("HH:mm:ss"),
                    moneda = "PEN",
                    tipo_operacion = "0101",
                    cliente_tipo_de_documento = cliente.NumeroDocumento.Length == 11 ? "6" : "1",
                    cliente_numero_de_documento = cliente.NumeroDocumento,
                    cliente_denominacion = string.IsNullOrWhiteSpace(cliente.RazonSocial)
                        ? $"{cliente.Nombres} {cliente.ApellidoPaterno} {cliente.ApellidoMaterno}".Trim()
                        : cliente.RazonSocial,
                    cliente_direccion = string.IsNullOrWhiteSpace(cliente.DireccionFiscal) ? "TRUJILLO" : cliente.DireccionFiscal,
                    total = total.ToString("0.00"),
                    items = new List<ApiSunatItemDto>
                    {
                        new ApiSunatItemDto
                        {
                            unidad_de_medida = "ZZ",
                            codigo_interno = "PAS-001",
                            descripcion = $"Pasaje de Transporte - Asiento #{pasaje.NumeroAsiento}",
                            cantidad = "1",
                            valor_unitario = valorUnitario6Dec.ToString("0.000000"),
                            porcentaje_igv = "18",
                            codigo_tipo_afectacion_igv = "10",
                            nombre_tributo = "IGV"
                        }
                    }
                };

                sunatResponse = await apiSunat.EmitirComprobanteAsync(sunatPayload);
            }

            var nuevoComprobante = new ComprobantesPago
            {
                ComprobanteId = Guid.NewGuid(),
                Documento = docTipo,
                Serie = serieObj.Serie,
                Numero = nuevoNumero,
                FechaDeEmision = DateOnly.FromDateTime(ahora),
                HoraDeEmision = TimeOnly.FromTimeSpan(ahora.TimeOfDay),
                Moneda = "PEN",
                TipoOperacion = "0101",
                ClienteId = cliente.ClienteId,
                ClienteTipoDeDocumento = cliente.NumeroDocumento.Length == 11 ? "6" : "1",
                ClienteNumeroDeDocumento = cliente.NumeroDocumento,
                ClienteDenominacion = string.IsNullOrWhiteSpace(cliente.RazonSocial)
                    ? $"{cliente.Nombres} {cliente.ApellidoPaterno} {cliente.ApellidoMaterno}".Trim()
                    : cliente.RazonSocial,
                ClienteDireccion = string.IsNullOrWhiteSpace(cliente.DireccionFiscal) ? "TRUJILLO" : cliente.DireccionFiscal,
                MontoGravado = montoGravado,
                MontoIgv = montoIgv,
                Total = total,
                EstadoSunat = esNotaVenta ? "COMPLETADO" : (sunatResponse.success ? "ACEPTADO" : "RECHAZADO"),
                RespuestaSunat = sunatResponse.message,
                PdfUrl = sunatResponse.file_pdf,
                XmlUrl = sunatResponse.file_xml,
                PasajeId = pasaje.PasajeId,
                UsuarioId = usuarioId,
                CreadoEn = DateTime.UtcNow
            };

            _context.ComprobantesPagos.Add(nuevoComprobante);

            var itemComprobante = new ItemsComprobante
            {
                ItemId = Guid.NewGuid(),
                ComprobanteId = nuevoComprobante.ComprobanteId,
                UnidadDeMedida = "ZZ",
                CodigoInterno = "PAS-001",
                Descripcion = $"Pasaje de Transporte - Asiento #{pasaje.NumeroAsiento}",
                Cantidad = 1,
                ValorUnitario = valorUnitario6Dec,
                PorcentajeIgv = 18,
                CodigoTipoAfectacionIgv = "10",
                NombreTributo = "IGV"
            };

            _context.ItemsComprobantes.Add(itemComprobante);

            if (sunatResponse.success)
            {
                pasaje.ComprobanteEmitido = true;
            }

            return (true, null);
        }

        #endregion

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
            public string? TipoComprobante { get; set; }
        }

        public class LiquidarReservaDto
        {
            public decimal MontoPago { get; set; }
            public string MedioPago { get; set; } = "EFECTIVO";
            public IFormFile? VoucherFile { get; set; }
            public string? TipoComprobante { get; set; }
        }

        #endregion
    }
}