using JJR.Api.DTOs;
using JJR.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Services
{
    public class TesoreriaService : ITesoreriaService
    {
        private readonly JjrDbContext _context;

        public TesoreriaService(JjrDbContext context)
        {
            _context = context;
        }

        public async Task<MovimientoCajaResponseDto> RegistrarMovimientoAsync(RegistrarMovimientoRequestDto dto, Guid usuarioId)
        {
            if (!dto.CuentaEmpresaId.HasValue || dto.CuentaEmpresaId == Guid.Empty)
            {
                throw new ArgumentException("Debe especificar una cuenta de empresa válida (CuentaEmpresaId).");
            }

            var movimiento = new MovimientosCaja
            {
                MovimientoId = Guid.NewGuid(),
                CuentaId = dto.CuentaEmpresaId.Value,
                UsuarioRegistroId = usuarioId,
                Monto = dto.Monto,
                Categoria = dto.TipoMovimiento.ToUpper(), // INGRESO, EGRESO, APERTURA, etc.
                Descripcion = dto.Concepto,
                FechaMovimiento = DateTime.Now
            };

            _context.MovimientosCajas.Add(movimiento);
            await _context.SaveChangesAsync();

            // Cargar datos de navegación para la respuesta
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            return new MovimientoCajaResponseDto
            {
                MovimientoId = movimiento.MovimientoId,
                FechaHora = movimiento.FechaMovimiento,
                TipoMovimiento = movimiento.Categoria,
                Concepto = movimiento.Descripcion,
                Monto = movimiento.Monto,
                MedioPago = dto.MedioPago,
                Referencia = dto.Referencia,
                UsuarioNombre = usuario != null ? $"{usuario.Nombres} {usuario.Apellidos}" : "N/A"
            };
        }

        public async Task<IEnumerable<MovimientoCajaResponseDto>> ListarMovimientosAsync(MovimientoFiltroDto filtro)
        {
            var query = _context.MovimientosCajas
                .Include(m => m.UsuarioRegistro)
                .Include(m => m.Cuenta)
                .AsQueryable();

            if (filtro.TipoMovimiento != null)
            {
                query = query.Where(m => m.Categoria.ToUpper() == filtro.TipoMovimiento.ToUpper());
            }

            if (filtro.FechaInicio.HasValue)
            {
                query = query.Where(m => m.FechaMovimiento >= filtro.FechaInicio.Value);
            }

            if (filtro.FechaFin.HasValue)
            {
                query = query.Where(m => m.FechaMovimiento <= filtro.FechaFin.Value);
            }

            return await query
                .OrderByDescending(m => m.FechaMovimiento)
                .Select(m => new MovimientoCajaResponseDto
                {
                    MovimientoId = m.MovimientoId,
                    FechaHora = m.FechaMovimiento,
                    TipoMovimiento = m.Categoria,
                    Concepto = m.Descripcion,
                    Monto = m.Monto,
                    MedioPago = "EFECTIVO",
                    Referencia = null,
                    UsuarioNombre = m.UsuarioRegistro != null
                        ? $"{m.UsuarioRegistro.Nombres} {m.UsuarioRegistro.Apellidos}"
                        : "N/A"
                })
                .ToListAsync();
        }

        public async Task<MovimientoCajaResponseDto?> ObtenerPorIdAsync(Guid movimientoId)
        {
            var m = await _context.MovimientosCajas
                .Include(x => x.UsuarioRegistro)
                .Include(x => x.Cuenta)
                .FirstOrDefaultAsync(x => x.MovimientoId == movimientoId);

            if (m == null) return null;

            return new MovimientoCajaResponseDto
            {
                MovimientoId = m.MovimientoId,
                FechaHora = m.FechaMovimiento,
                TipoMovimiento = m.Categoria,
                Concepto = m.Descripcion,
                Monto = m.Monto,
                MedioPago = "EFECTIVO",
                Referencia = null,
                UsuarioNombre = m.UsuarioRegistro != null
                    ? $"{m.UsuarioRegistro.Nombres} {m.UsuarioRegistro.Apellidos}"
                    : "N/A"
            };
        }
    }
}