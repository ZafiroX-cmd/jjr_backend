using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public DashboardController(JjrDbContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/resumen
        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen()
        {
            var hoyUtc = DateTime.UtcNow.Date;

            // 1. Métricas generales para las tarjetas superiores (counts)
            var totalViajesProgramados = await _context.Viajes.CountAsync(v => v.Estado == "Programado");
            var pasajesVendidosHoy = await _context.Pasajes.CountAsync(p => p.CreadoEn >= hoyUtc && p.Estado != "Anulado");
            var encomiendasHoy = await _context.Encomiendas.CountAsync(e => e.CreadoEn >= hoyUtc && e.Estado != "Anulado");
            var totalFlota = await _context.Vehiculos.CountAsync(v => v.Activo);

            // 2. Estado operativo de la Flota (fleet)
            var enRuta = await _context.Vehiculos.CountAsync(v => v.Estado == "En Ruta" && v.Activo);
            var enTaller = await _context.Vehiculos.CountAsync(v => v.Estado == "En Taller" && v.Activo);
            var fueraDeServicio = await _context.Vehiculos.CountAsync(v => (v.Estado == "Mantenimiento" || v.Estado == "Inactivo") && v.Activo);

            // 3. Últimos movimientos de caja / actividad reciente
            var ultimosMovimientos = await _context.MovimientosCajas
                .Include(m => m.UsuarioRegistro)
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(5)
                .Select(m => new
                {
                    movimientoId = m.MovimientoId,
                    tipo = m.TipoMovimiento,
                    categoria = m.Categoria,
                    descripcion = m.Descripcion,
                    monto = m.Monto,
                    fecha = m.FechaMovimiento,
                    usuario = m.UsuarioRegistro != null ? m.UsuarioRegistro.Username : "Sistema"
                })
                .ToListAsync();

            // 4. Salidas de viajes próximos
            var proximosViajes = await _context.Viajes
                .Include(v => v.Ruta)
                .Include(v => v.Vehiculo)
                .Include(v => v.Conductor)
                .Where(v => v.FechaSalida >= DateTime.UtcNow && v.Estado == "Programado")
                .OrderBy(v => v.FechaSalida)
                .Take(5)
                .Select(v => new
                {
                    viajeId = v.ViajeId,
                    ruta = v.Ruta.Origen + " - " + v.Ruta.Destino,
                    placa = v.Vehiculo.Placa,
                    conductor = $"{v.Conductor.Nombres} {v.Conductor.ApellidoPaterno}",
                    fechaSalida = v.FechaSalida,
                    asientosOcupados = v.Pasajes.Count(p => p.Estado != "Anulado")
                })
                .ToListAsync();

            return Ok(new
            {
                counts = new
                {
                    viajes = totalViajesProgramados,
                    pasajes = pasajesVendidosHoy,
                    encomiendas = encomiendasHoy,
                    flota = totalFlota
                },
                fleet = new
                {
                    onRoute = enRuta,
                    inWorkshop = enTaller,
                    outOfService = fueraDeServicio
                },
                ultimosMovimientos,
                proximosViajes,
                updatedAt = DateTime.UtcNow
            });
        }
    }
}