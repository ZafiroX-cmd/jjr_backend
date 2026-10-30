using JJR.Api.Dtos;
using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JJR.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LiquidacionesController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public LiquidacionesController(JjrDbContext context)
        {
            _context = context;
        }

        // GET: api/Liquidaciones/viajes-pendientes
        [HttpGet("viajes-pendientes")]
        public async Task<IActionResult> ObtenerViajesPendientesLiquidacion()
        {
            var viajes = await _context.Viajes
                .Include(v => v.Vehiculo)
                    .ThenInclude(veh => veh.Socio)
                .Include(v => v.Ruta)
                .Where(v => v.Vehiculo != null && v.Vehiculo.SocioId != null)
                .OrderByDescending(v => v.FechaSalida)
                .ToListAsync();

            var liquidacionesExistentes = await _context.LiquidacionesSocios
                .Select(l => l.ViajeId)
                .ToListAsync();

            var previaLista = new List<PreviaLiquidacionViajeDto>();

            foreach (var viaje in viajes)
            {
                var socio = viaje.Vehiculo.Socio!;
                bool yaLiquidado = liquidacionesExistentes.Contains(viaje.ViajeId);

                // 1. Ingresos por pasajes
                decimal ingresosPasajes = await _context.Pasajes
                    .Where(p => p.ViajeId == viaje.ViajeId && p.Estado != "Anulado")
                    .SumAsync(p => (decimal?)p.MontoAdelanto) ?? 0m;

                // 2. Ingresos por encomiendas
                decimal ingresosEncomiendas = await _context.Encomiendas
                    .Where(e => e.ViajeId == viaje.ViajeId && e.Estado != "ANULADO")
                    .SumAsync(e => (decimal?)e.MontoCobro) ?? 0m;

                // 3. Egresos operativos deducibles
                decimal egresosDeducibles = await _context.EgresosOperativos
                    .Where(e => e.ViajeId == viaje.ViajeId || (e.VehiculoId == viaje.VehiculoId && e.CreadoEn.Date == viaje.FechaSalida.Date))
                    .SumAsync(e => (decimal?)e.Monto) ?? 0m;

                decimal brutoTotal = (ingresosPasajes + ingresosEncomiendas) - egresosDeducibles;
                if (brutoTotal < 0) brutoTotal = 0;

                decimal pctSocio = socio.PorcentajeGanancia;
                decimal gananciaSocio = Math.Round(brutoTotal * (pctSocio / 100m), 2);
                decimal gananciaEmpresa = brutoTotal - gananciaSocio;

                previaLista.Add(new PreviaLiquidacionViajeDto
                {
                    ViajeId = viaje.ViajeId,
                    SocioId = socio.SocioId,
                    NombreSocio = socio.RazonSocialNombre,
                    PlacaVehiculo = viaje.Vehiculo.Placa,
                    RutaOrigenDestino = viaje.Ruta != null ? $"{viaje.Ruta.Origen} - {viaje.Ruta.Destino}" : "Sin Ruta",
                    FechaSalida = viaje.FechaSalida,
                    MontoIngresoPasajes = ingresosPasajes,
                    MontoIngresoEncomiendas = ingresosEncomiendas,
                    MontoEgresosDeducibles = egresosDeducibles,
                    MontoBrutoTotal = brutoTotal,
                    PorcentajeSocio = pctSocio,
                    MontoGananciaSocio = gananciaSocio,
                    MontoGananciaEmpresa = gananciaEmpresa,
                    YaLiquidado = yaLiquidado
                });
            }

            return Ok(previaLista);
        }

        // POST: api/Liquidaciones/procesar
        [HttpPost("procesar")]
        public async Task<IActionResult> ProcesarLiquidacion([FromBody] ProcesarLiquidacionRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var yaLiquidado = await _context.LiquidacionesSocios.AnyAsync(l => l.ViajeId == dto.ViajeId);
            if (yaLiquidado)
            {
                return BadRequest(new { mensaje = "El viaje seleccionado ya cuenta con una liquidación registrada." });
            }

            var viaje = await _context.Viajes
                .Include(v => v.Vehiculo)
                    .ThenInclude(veh => veh.Socio)
                .Include(v => v.Ruta)
                .FirstOrDefaultAsync(v => v.ViajeId == dto.ViajeId);

            if (viaje == null || viaje.Vehiculo == null || viaje.Vehiculo.Socio == null)
            {
                return BadRequest(new { mensaje = "No se encontró el viaje o la unidad no posee un socio asignado." });
            }

            var cuenta = await _context.CuentasEmpresas.FindAsync(dto.CuentaId);
            if (cuenta == null || !cuenta.Activo)
            {
                return BadRequest(new { mensaje = "La caja/cuenta seleccionada para el pago no es válida." });
            }

            var socio = viaje.Vehiculo.Socio;

            // Recalcular montos
            decimal ingresosPasajes = await _context.Pasajes
                .Where(p => p.ViajeId == viaje.ViajeId && p.Estado != "Anulado")
                .SumAsync(p => (decimal?)p.MontoAdelanto) ?? 0m;

            decimal ingresosEncomiendas = await _context.Encomiendas
                .Where(e => e.ViajeId == viaje.ViajeId && e.Estado != "ANULADO")
                .SumAsync(e => (decimal?)e.MontoCobro) ?? 0m;

            decimal egresosDeducibles = await _context.EgresosOperativos
                .Where(e => e.ViajeId == viaje.ViajeId || (e.VehiculoId == viaje.VehiculoId && e.CreadoEn.Date == viaje.FechaSalida.Date))
                .SumAsync(e => (decimal?)e.Monto) ?? 0m;

            decimal brutoTotal = (ingresosPasajes + ingresosEncomiendas) - egresosDeducibles;
            if (brutoTotal < 0) brutoTotal = 0;

            decimal pctSocio = socio.PorcentajeGanancia;
            decimal gananciaSocio = Math.Round(brutoTotal * (pctSocio / 100m), 2);
            decimal gananciaEmpresa = brutoTotal - gananciaSocio;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid usuarioId = Guid.Empty;
            if (!string.IsNullOrEmpty(userIdStr)) Guid.TryParse(userIdStr, out usuarioId);

            DateTime ahora = DateTime.UtcNow;

            // 1. Guardar Liquidación
            var liquidacion = new LiquidacionesSocio
            {
                LiquidacionId = Guid.NewGuid(),
                SocioId = socio.SocioId,
                ViajeId = viaje.ViajeId,
                MontoIngresoPasajes = ingresosPasajes,
                MontoIngresoEncomiendas = ingresosEncomiendas,
                MontoEgresosDeducibles = egresosDeducibles,
                MontoBrutoTotal = brutoTotal,
                PorcentajeSocio = pctSocio,
                MontoGananciaSocio = gananciaSocio,
                MontoGananciaEmpresa = gananciaEmpresa,
                EstadoPago = "PAGADO",
                FechaPago = ahora,
                ProcesadoEn = ahora
            };

            _context.LiquidacionesSocios.Add(liquidacion);

            // 2. Registrar egreso en Tesorería y descontar saldo
            if (gananciaSocio > 0)
            {
                var movimientoTesoreria = new MovimientosCaja
                {
                    MovimientoId = Guid.NewGuid(),
                    CuentaId = dto.CuentaId,
                    Monto = gananciaSocio,
                    Categoria = "LIQUIDACION_SOCIO",
                    SocioId = socio.SocioId,
                    Descripcion = $"Pago Liquidación Viaje ({viaje.FechaSalida:dd/MM/yyyy}) - Placa {viaje.Vehiculo.Placa} - Socio {socio.RazonSocialNombre}",
                    UsuarioRegistroId = usuarioId,
                    FechaMovimiento = ahora
                };

                _context.MovimientosCajas.Add(movimientoTesoreria);
                cuenta.SaldoActual -= gananciaSocio;
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Liquidación procesada y abonada correctamente al socio.", liquidacionId = liquidacion.LiquidacionId });
        }

        // GET: api/Liquidaciones/historial
        [HttpGet("historial")]
        public async Task<IActionResult> ObtenerHistorial()
        {
            var historial = await _context.LiquidacionesSocios
                .Include(l => l.Socio)
                .Include(l => l.Viaje)
                    .ThenInclude(v => v.Vehiculo)
                .Include(l => l.Viaje)
                    .ThenInclude(v => v.Ruta)
                .OrderByDescending(l => l.ProcesadoEn)
                .Select(l => new LiquidacionSocioResponseDto
                {
                    LiquidacionId = l.LiquidacionId,
                    ViajeId = l.ViajeId,
                    NombreSocio = l.Socio.RazonSocialNombre,
                    PlacaVehiculo = l.Viaje.Vehiculo != null ? l.Viaje.Vehiculo.Placa : "N/A",
                    RutaOrigenDestino = l.Viaje.Ruta != null ? $"{l.Viaje.Ruta.Origen} - {l.Viaje.Ruta.Destino}" : "N/A",
                    MontoBrutoTotal = l.MontoBrutoTotal,
                    PorcentajeSocio = l.PorcentajeSocio,
                    MontoGananciaSocio = l.MontoGananciaSocio,
                    MontoGananciaEmpresa = l.MontoGananciaEmpresa,
                    EstadoPago = l.EstadoPago,
                    FechaPago = l.FechaPago,
                    ProcesadoEn = l.ProcesadoEn
                })
                .ToListAsync();

            return Ok(historial);
        }
    }
}