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
    public class EgresosOperativosController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public EgresosOperativosController(JjrDbContext context)
        {
            _context = context;
        }

        // GET: api/EgresosOperativos
        [HttpGet]
        public async Task<IActionResult> GetEgresos(
            [FromQuery] string? categoria,
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {
            var query = _context.EgresosOperativos
                .Include(e => e.Vehiculo)
                .Include(e => e.RegistradoPorUsuario)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(e => e.Categoria == categoria);
            }

            if (fechaInicio.HasValue)
            {
                query = query.Where(e => e.CreadoEn >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                query = query.Where(e => e.CreadoEn <= fechaFin.Value.AddDays(1));
            }

            var lista = await query
                .OrderByDescending(e => e.CreadoEn)
                .Select(e => new
                {
                    egresoId = e.EgresoId,
                    categoria = e.Categoria,
                    monto = e.Monto,
                    descripcion = e.Descripcion,
                    comprobanteUrl = e.UrlFotoComprobante,
                    validadoPorAdmin = e.ValidadoPorAdmin,
                    placaVehiculo = e.Vehiculo != null ? e.Vehiculo.Placa : null,
                    usuarioNombre = e.RegistradoPorUsuario != null ? e.RegistradoPorUsuario.Username : "Sistema",
                    fechaEgreso = e.CreadoEn
                })
                .ToListAsync();

            return Ok(lista);
        }

        // POST: api/EgresosOperativos
        [HttpPost]
        public async Task<IActionResult> RegistrarEgreso([FromForm] RegistrarEgresoDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid usuarioId = Guid.Empty;
            if (!string.IsNullOrEmpty(userIdStr)) Guid.TryParse(userIdStr, out usuarioId);

            // Validar existencia de la cuenta activa para afectar en Tesorería
            var cuenta = await _context.CuentasEmpresas.FindAsync(dto.CuentaId);
            if (cuenta == null || !cuenta.Activo)
            {
                return BadRequest(new { mensaje = "La cuenta o caja seleccionada no existe o no está activa." });
            }

            // Procesar adjunto de comprobante si existe
            string? comprobanteUrl = null;
            if (dto.ComprobanteFile != null && dto.ComprobanteFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "egresos");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.ComprobanteFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ComprobanteFile.CopyToAsync(stream);
                }

                comprobanteUrl = $"/uploads/egresos/{fileName}";
            }

            // 1. Crear el Egreso Operativo
            var nuevoEgreso = new EgresosOperativo
            {
                EgresoId = Guid.NewGuid(),
                VehiculoId = dto.VehiculoId,
                ViajeId = dto.ViajeId,
                Categoria = dto.Categoria.ToUpper(),
                Monto = dto.Monto,
                Descripcion = dto.Descripcion ?? string.Empty,
                UrlFotoComprobante = comprobanteUrl,
                ValidadoPorAdmin = false,
                RegistradoPorUsuarioId = usuarioId,
                CreadoEn = DateTime.UtcNow
            };

            _context.EgresosOperativos.Add(nuevoEgreso);

            // 2. Registro Automático en Tesorería (MovimientosCaja)
            var movimientoTesoreria = new MovimientosCaja
            {
                MovimientoId = Guid.NewGuid(),
                CuentaId = dto.CuentaId,
                Monto = dto.Monto,
                TipoMovimiento = "EGRESO", // 👈 String en lugar de enum
                Categoria = dto.Categoria.ToUpper(),
                Descripcion = $"EGRESO OPERATIVO - {dto.Categoria.ToUpper()}: {dto.Descripcion}",
                UsuarioRegistroId = usuarioId,
                FechaMovimiento = DateTime.UtcNow
            };

            _context.MovimientosCajas.Add(movimientoTesoreria);

            // 3. Descontar saldo de la caja/cuenta
            cuenta.SaldoActual -= dto.Monto;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Egreso operativo registrado correctamente.", egresoId = nuevoEgreso.EgresoId });
        }
    }

    #region --- DTOs ---

    public class RegistrarEgresoDto
    {
        public Guid CuentaId { get; set; }
        public Guid? VehiculoId { get; set; }
        public Guid? ViajeId { get; set; }
        public string Categoria { get; set; } = "COMBUSTIBLE";
        public decimal Monto { get; set; }
        public string? Descripcion { get; set; }
        public IFormFile? ComprobanteFile { get; set; }
    }

    #endregion
}