using JJR.Api.Dtos;
using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public ReportesController(JjrDbContext context)
        {
            _context = context;
        }

        [HttpGet("financiero")]
        public async Task<IActionResult> ObtenerReporteFinanciero([FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin)
        {
            // Rango predeterminado: Últimos 30 días si no se especifican fechas
            var inicio = fechaInicio?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-30);
            var fin = fechaFin?.ToUniversalTime() ?? DateTime.UtcNow;

            // 1. Ingresos desde MovimientosCaja (Monto > 0)
            var queryIngresos = _context.MovimientosCajas
                .Where(m => m.FechaMovimiento >= inicio && m.FechaMovimiento <= fin && m.Monto > 0);

            var totalIngresos = await queryIngresos.SumAsync(m => (decimal?)m.Monto) ?? 0m;

            var ingresosPorCatRaw = await queryIngresos
                .GroupBy(m => string.IsNullOrEmpty(m.Categoria) ? "OTROS_INGRESOS" : m.Categoria)
                .Select(g => new { Categoria = g.Key, Monto = g.Sum(x => x.Monto) })
                .ToListAsync();

            // 2. Egresos Operativos desde EgresosOperativos
            var queryEgresos = _context.EgresosOperativos
                .Where(e => e.CreadoEn >= inicio && e.CreadoEn <= fin);

            var totalEgresos = await queryEgresos.SumAsync(e => (decimal?)e.Monto) ?? 0m;

            var egresosPorCatRaw = await queryEgresos
                .GroupBy(e => string.IsNullOrEmpty(e.Categoria) ? "GASTOS_VARIOS" : e.Categoria)
                .Select(g => new { Categoria = g.Key, Monto = g.Sum(x => x.Monto) })
                .ToListAsync();

            // 3. Volúmenes de Operación reales
            var totalPasajes = await _context.Pasajes
                .CountAsync(p => p.CreadoEn >= inicio && p.CreadoEn <= fin && p.Estado != "Anulado");

            var totalEncomiendas = await _context.Encomiendas
                .CountAsync(e => e.CreadoEn >= inicio && e.CreadoEn <= fin && e.Estado != "ANULADO");

            var response = new ReporteFinancieroResponseDto
            {
                TotalIngresos = totalIngresos,
                TotalEgresos = totalEgresos,
                UtilidadNeta = totalIngresos - totalEgresos,
                TotalPasajesVendidos = totalPasajes,
                TotalEncomiendasProcesadas = totalEncomiendas,
                IngresosPorCategoria = ingresosPorCatRaw.Select(i => new DesgloseCategoriaDto
                {
                    Categoria = i.Categoria,
                    Monto = i.Monto,
                    Porcentaje = totalIngresos > 0 ? Math.Round((i.Monto / totalIngresos) * 100, 2) : 0
                }).OrderByDescending(i => i.Monto).ToList(),
                EgresosPorCategoria = egresosPorCatRaw.Select(e => new DesgloseCategoriaDto
                {
                    Categoria = e.Categoria,
                    Monto = e.Monto,
                    Porcentaje = totalEgresos > 0 ? Math.Round((e.Monto / totalEgresos) * 100, 2) : 0
                }).OrderByDescending(e => e.Monto).ToList()
            };

            return Ok(response);
        }
    }
}