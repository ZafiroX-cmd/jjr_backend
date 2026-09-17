using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NominaController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public NominaController(JjrDbContext context)
        {
            _context = context;
        }

        #region --- CONTRATOS LABORALES ---

        // GET: api/Nomina/contratos
        [HttpGet("contratos")]
        public async Task<IActionResult> GetContratos()
        {
            var contratos = await _context.ContratosLaborales
                .Include(c => c.Usuario)
                    .ThenInclude(u => u!.Rol)
                .Include(c => c.Conductor)
                .Include(c => c.Socio)
                .OrderByDescending(c => c.Activo)
                .ThenByDescending(c => c.FechaInicio)
                .Select(c => new
                {
                    c.ContratoId,
                    TipoTrabajador = c.ConductorId != null ? "Conductor" :
                                     (c.UsuarioId != null ? "Administrativo / Counter" :
                                     (c.SocioId != null ? "Socio Inversionista" : "General")),
                    TrabajadorNombre = c.Conductor != null
                        ? $"{c.Conductor.Nombres} {c.Conductor.ApellidoPaterno} {c.Conductor.ApellidoMaterno}"
                        : (c.Usuario != null ? $"{c.Usuario.Nombres} {c.Usuario.Apellidos}"
                        : (c.Socio != null ? c.Socio.RazonSocialNombre : "Sin Asignar")),
                    Documento = c.Conductor != null
                        ? c.Conductor.NumeroDocumento
                        : (c.Usuario != null ? (c.Usuario.Dni ?? c.Usuario.Username)
                        : (c.Socio != null ? c.Socio.RucDni : "—")),
                    Cargo = c.Conductor != null ? "Conductor Chofer" :
                            (c.Usuario != null ? c.Usuario.Rol.Nombre : "Socio / Inversionista"),
                    c.SueldoBase,
                    c.PagoPorViaje,
                    TipoPago = c.PagoPorViaje.HasValue && c.PagoPorViaje > 0 ? "Por Viaje + Base" : "Mensual",
                    c.FechaInicio,
                    c.Activo,
                    c.ConductorId,
                    c.UsuarioId,
                    c.SocioId
                })
                .ToListAsync();

            return Ok(contratos);
        }

        // POST: api/Nomina/contratos
        [HttpPost("contratos")]
        public async Task<IActionResult> CreateContrato([FromBody] ContratoDto model)
        {
            string tipo = model.ConductorId != null ? "Conductor" :
                          model.UsuarioId != null ? "Administrativo" :
                          model.SocioId != null ? "Socio" : "General";

            var contrato = new ContratosLaborale
            {
                ConductorId = model.ConductorId,
                UsuarioId = model.UsuarioId,
                SocioId = model.SocioId,
                TipoEmpleado = tipo, 
                SueldoBase = model.SueldoBase,
                PagoPorViaje = model.PagoPorViaje,
                FechaInicio = model.FechaInicio,
                Activo = true,
                CreadoEn = DateTime.UtcNow
            };

            _context.ContratosLaborales.Add(contrato);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Contrato laboral registrado con éxito.", contratoId = contrato.ContratoId });
        }

        // PUT: api/Nomina/contratos/{id}
        [HttpPut("contratos/{id}")]
        public async Task<IActionResult> UpdateContrato(Guid id, [FromBody] ContratoDto model)
        {
            var contrato = await _context.ContratosLaborales.FindAsync(id);
            if (contrato == null) return NotFound(new { mensaje = "El contrato no existe." });

            contrato.ConductorId = model.ConductorId;
            contrato.UsuarioId = model.UsuarioId;
            contrato.SocioId = model.SocioId;
            contrato.SueldoBase = model.SueldoBase;
            contrato.PagoPorViaje = model.PagoPorViaje;
            contrato.FechaInicio = model.FechaInicio;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Contrato actualizado correctamente." });
        }

        // PATCH: api/Nomina/contratos/{id}/estado
        [HttpPatch("contratos/{id}/estado")]
        public async Task<IActionResult> ToggleEstadoContrato(Guid id, [FromBody] CambiarEstadoDto model)
        {
            var contrato = await _context.ContratosLaborales.FindAsync(id);
            if (contrato == null) return NotFound(new { mensaje = "El contrato no existe." });

            contrato.Activo = model.Estado != "Inactivo";
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estado del contrato actualizado correctamente." });
        }

        #endregion

        #region --- PAGOS DE NÓMINA ---

        // GET: api/Nomina/pagos
        [HttpGet("pagos")]
        public async Task<IActionResult> GetPagosNomina()
        {
            var pagos = await _context.PagosNominas
                .Include(p => p.Contrato)
                    .ThenInclude(c => c!.Conductor)
                .Include(p => p.Contrato)
                    .ThenInclude(c => c!.Usuario)
                .Include(p => p.Contrato)
                    .ThenInclude(c => c!.Socio)
                .OrderByDescending(p => p.PeriodoAnio)
                .ThenByDescending(p => p.PeriodoMes)
                .Select(p => new
                {
                    p.PagoId,
                    p.ContratoId,
                    TrabajadorNombre = p.Contrato.Conductor != null
                        ? $"{p.Contrato.Conductor.Nombres} {p.Contrato.Conductor.ApellidoPaterno}"
                        : (p.Contrato.Usuario != null ? $"{p.Contrato.Usuario.Nombres} {p.Contrato.Usuario.Apellidos}"
                        : (p.Contrato.Socio != null ? p.Contrato.Socio.RazonSocialNombre : "Sin Asignar")),
                    Periodo = $"{p.PeriodoAnio}-{p.PeriodoMes:D2}",
                    p.PeriodoMes,
                    p.PeriodoAnio,
                    p.SueldoGanado,
                    TotalDescuentos = p.TotalDescuentos ?? 0,
                    TotalAdelantos = p.TotalAdelantos ?? 0,
                    p.MontoNetoPagar,
                    p.FechaPago,
                    EstadoPago = p.FechaPago.HasValue ? "Pagado" : "Pendiente",
                    Observaciones = p.ObservacionPago
                })
                .ToListAsync();

            return Ok(pagos);
        }

        // POST: api/Nomina/pagos
        [HttpPost("pagos")]
        public async Task<IActionResult> CreatePagoNomina([FromBody] PagoNominaDto model)
        {
            var contrato = await _context.ContratosLaborales.FindAsync(model.ContratoId);
            if (contrato == null) return BadRequest(new { mensaje = "El contrato especificado no existe." });

            if (await _context.PagosNominas.AnyAsync(p => p.ContratoId == model.ContratoId
                && p.PeriodoMes == model.PeriodoMes
                && p.PeriodoAnio == model.PeriodoAnio))
            {
                return BadRequest(new { mensaje = $"Ya existe una planilla registrada para este trabajador en el periodo {model.PeriodoAnio}-{model.PeriodoMes:D2}." });
            }

            var pago = new PagosNomina
            {
                ContratoId = model.ContratoId,
                PeriodoMes = model.PeriodoMes,
                PeriodoAnio = model.PeriodoAnio,
                SueldoGanado = model.SueldoGanado > 0 ? model.SueldoGanado : contrato.SueldoBase,
                TotalDescuentos = model.TotalDescuentos,
                TotalAdelantos = model.TotalAdelantos,
                MontoNetoPagar = (model.SueldoGanado > 0 ? model.SueldoGanado : contrato.SueldoBase) - model.TotalDescuentos - model.TotalAdelantos,
                FechaPago = model.MarcarComoPagado ? DateTime.UtcNow : null,
                ObservacionPago = model.Observaciones?.Trim(),
                CreadoEn = DateTime.UtcNow
            };

            _context.PagosNominas.Add(pago);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Boleta / Pago de nómina generado correctamente.", pagoId = pago.PagoId });
        }

        // PATCH: api/Nomina/pagos/{id}/estado
        [HttpPatch("pagos/{id}/estado")]
        public async Task<IActionResult> ProcessPagoNomina(Guid id, [FromBody] CambiarEstadoDto model)
        {
            var pago = await _context.PagosNominas.FindAsync(id);
            if (pago == null) return NotFound(new { mensaje = "El registro de pago no existe." });

            if (model.Estado == "Pagado")
            {
                pago.FechaPago = DateTime.UtcNow;
            }
            else
            {
                pago.FechaPago = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Estado del pago actualizado correctamente a {model.Estado}." });
        }

        #endregion
    }

    #region --- DTOs ---

    public class ContratoDto
    {
        public Guid? ConductorId { get; set; }
        public Guid? UsuarioId { get; set; }
        public Guid? SocioId { get; set; }
        public decimal SueldoBase { get; set; }
        public decimal? PagoPorViaje { get; set; }
        public DateOnly FechaInicio { get; set; }
    }

    public class PagoNominaDto
    {
        public Guid ContratoId { get; set; }
        public int PeriodoMes { get; set; }
        public int PeriodoAnio { get; set; }
        public decimal SueldoGanado { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal TotalAdelantos { get; set; }
        public bool MarcarComoPagado { get; set; }
        public string? Observaciones { get; set; }
    }

    #endregion
}