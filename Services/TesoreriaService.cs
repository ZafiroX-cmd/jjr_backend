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

        public async Task<IEnumerable<CuentaEmpresaDto>> ListarCuentasAsync()
        {
            return await _context.CuentasEmpresas
                .Where(c => c.Activo)
                .Select(c => new CuentaEmpresaDto
                {
                    CuentaId = c.CuentaId,
                    NombreCuenta = c.NombreCuenta,
                    TipoCuenta = c.TipoCuenta,
                    SaldoActual = c.SaldoActual,
                    Moneda = c.Moneda
                })
                .ToListAsync();
        }

        public async Task<MovimientoCajaResponseDto> RegistrarMovimientoAsync(RegistrarMovimientoRequestDto dto, Guid usuarioId)
        {
            if (!dto.CuentaEmpresaId.HasValue || dto.CuentaEmpresaId == Guid.Empty)
            {
                throw new ArgumentException("Debe especificar una cuenta de empresa válida (CuentaEmpresaId).");
            }

            if (string.IsNullOrWhiteSpace(dto.TipoMovimiento)
                || !Enum.TryParse(dto.TipoMovimiento.Trim(), ignoreCase: true, out TipoMovimientoCaja tipoMovimiento))
            {
                throw new ArgumentException("TipoMovimiento debe ser INGRESO o EGRESO.");
            }

            var movimiento = new MovimientosCaja
            {
                MovimientoId = Guid.NewGuid(),
                CuentaId = dto.CuentaEmpresaId.Value,
                UsuarioRegistroId = usuarioId,
                Monto = dto.Monto,
                TipoMovimiento = tipoMovimiento,
                Categoria = "TESORERIA",
                Descripcion = dto.Concepto,
                FechaMovimiento = DateTime.Now
            };

            _context.MovimientosCajas.Add(movimiento);

            // Actualizar saldo de la cuenta según el tipo de movimiento
            var cuenta = await _context.CuentasEmpresas.FindAsync(dto.CuentaEmpresaId.Value);
            if (cuenta != null)
            {
                if (tipoMovimiento == TipoMovimientoCaja.INGRESO)
                {
                    cuenta.SaldoActual += dto.Monto;
                }
                else if (tipoMovimiento == TipoMovimientoCaja.EGRESO)
                {
                    cuenta.SaldoActual -= dto.Monto;
                }
            }

            await _context.SaveChangesAsync();

            // Cargar datos de navegación para la respuesta
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            return new MovimientoCajaResponseDto
            {
                MovimientoId = movimiento.MovimientoId,
                FechaHora = movimiento.FechaMovimiento,
                TipoMovimiento = movimiento.TipoMovimiento.ToString(),
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

            if (!string.IsNullOrWhiteSpace(filtro.TipoMovimiento)
                && Enum.TryParse(filtro.TipoMovimiento.Trim(), ignoreCase: true, out TipoMovimientoCaja tipoFiltro))
            {
                query = query.Where(m => m.TipoMovimiento == tipoFiltro);
            }

            if (filtro.FechaInicio.HasValue)
            {
                query = query.Where(m => m.FechaMovimiento >= filtro.FechaInicio.Value);
            }

            if (filtro.FechaFin.HasValue)
            {
                query = query.Where(m => m.FechaMovimiento <= filtro.FechaFin.Value.AddDays(1));
            }

            return await query
                .OrderByDescending(m => m.FechaMovimiento)
                .Select(m => new MovimientoCajaResponseDto
                {
                    MovimientoId = m.MovimientoId,
                    FechaHora = m.FechaMovimiento,
                    TipoMovimiento = m.TipoMovimiento == TipoMovimientoCaja.INGRESO ? "INGRESO" : "EGRESO",
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
                TipoMovimiento = m.TipoMovimiento.ToString(),
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