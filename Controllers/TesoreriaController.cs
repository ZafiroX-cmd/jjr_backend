using System.Security.Claims;
using JJR.Api.DTOs;
using JJR.Api.Models;
using JJR.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TesoreriaController : ControllerBase
    {
        private readonly ITesoreriaService _tesoreriaService;
        private readonly JjrDbContext _context;

        public TesoreriaController(ITesoreriaService tesoreriaService, JjrDbContext context)
        {
            _tesoreriaService = tesoreriaService;
            _context = context;
        }

        // GET: api/tesoreria/cuentas (Habilitado para cargar el desplegable en Egresos y Tesorería)
        [HttpGet("cuentas")]
        public async Task<IActionResult> ListarCuentas()
        {
            var cuentas = await _context.CuentasEmpresas
                .Where(c => c.Activo)
                .Select(c => new
                {
                    cuentaId = c.CuentaId,
                    nombreCuenta = c.NombreCuenta,
                    tipoCuenta = c.TipoCuenta,
                    saldoActual = c.SaldoActual,
                    moneda = c.Moneda
                })
                .ToListAsync();

            return Ok(cuentas);
        }

        [HttpPost("movimientos")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] RegistrarMovimientoRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var usuarioId))
            {
                return Unauthorized("Identificador de usuario inválido.");
            }

            try
            {
                var resultado = await _tesoreriaService.RegistrarMovimientoAsync(dto, usuarioId);
                return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.MovimientoId }, resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("movimientos")]
        public async Task<IActionResult> ListarMovimientos([FromQuery] MovimientoFiltroDto filtro)
        {
            var movimientos = await _tesoreriaService.ListarMovimientosAsync(filtro);
            return Ok(movimientos);
        }

        [HttpGet("movimientos/{id:guid}")]
        public async Task<IActionResult> ObtenerPorId(Guid id)
        {
            var movimiento = await _tesoreriaService.ObtenerPorIdAsync(id);
            if (movimiento == null)
            {
                return NotFound("No se encontró el movimiento de caja solicitado.");
            }

            return Ok(movimiento);
        }
    }
}