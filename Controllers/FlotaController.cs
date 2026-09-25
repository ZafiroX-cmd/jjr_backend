using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FlotaController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public FlotaController(JjrDbContext context)
        {
            _context = context;
        }

        #region --- VEHÍCULOS ---

        // GET: api/Flota/vehiculos
        [HttpGet("vehiculos")]
        public async Task<IActionResult> GetVehiculos([FromQuery] int? oficinaId)
        {
            var query = _context.Vehiculos
                .Include(v => v.Socio)
                .AsQueryable();

            if (oficinaId.HasValue && oficinaId > 0)
            {
                query = query.Where(v => v.OficinaActualId == oficinaId.Value);
            }

            var vehiculos = await query
                .OrderByDescending(v => v.Activo)
                .ThenBy(v => v.Placa)
                .Select(v => new
                {
                    v.VehiculoId,
                    v.Placa,
                    v.Marca,
                    v.Modelo,
                    v.CapacidadAsientos,
                    v.CapacidadCargaKg,
                    v.SoatVencimiento,
                    v.RevisionTecnicaVencimiento,
                    v.Estado,
                    v.Activo,
                    v.OficinaActualId,
                    SocioPropietario = v.Socio != null ? v.Socio.RazonSocialNombre : "Vehículo Propio (JJR S.A.C.)",
                    SocioId = v.SocioId,
                    SoatPorVencer = v.SoatVencimiento.HasValue && v.SoatVencimiento.Value <= DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
                    RevisionPorVencer = v.RevisionTecnicaVencimiento.HasValue && v.RevisionTecnicaVencimiento.Value <= DateOnly.FromDateTime(DateTime.Today.AddDays(15))
                })
                .ToListAsync();

            return Ok(vehiculos);
        }

        // POST: api/Flota/vehiculos
        [HttpPost("vehiculos")]
        public async Task<IActionResult> CreateVehiculo([FromBody] VehiculoDto model)
        {
            if (await _context.Vehiculos.AnyAsync(v => v.Placa == model.Placa.ToUpper().Trim()))
            {
                return BadRequest(new { mensaje = $"La placa {model.Placa} ya se encuentra registrada." });
            }

            var vehiculo = new Vehiculo
            {
                Placa = model.Placa.ToUpper().Trim(),
                Marca = model.Marca.Trim(),
                Modelo = model.Modelo.Trim(),
                CapacidadAsientos = model.CapacidadAsientos,
                CapacidadCargaKg = model.CapacidadCargaKg,
                SocioId = model.SocioId,
                SoatVencimiento = model.SoatVencimiento,
                RevisionTecnicaVencimiento = model.RevisionTecnicaVencimiento,
                OficinaActualId = model.OficinaActualId ?? 1, // Trujillo por defecto
                Estado = "Disponible",
                Activo = true
            };

            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Vehículo registrado correctamente.", vehiculoId = vehiculo.VehiculoId });
        }

        // PUT: api/Flota/vehiculos/{id}
        [HttpPut("vehiculos/{id}")]
        public async Task<IActionResult> UpdateVehiculo(Guid id, [FromBody] VehiculoDto model)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo == null)
            {
                return NotFound(new { mensaje = "El vehículo solicitado no existe." });
            }

            if (await _context.Vehiculos.AnyAsync(v => v.Placa == model.Placa.ToUpper().Trim() && v.VehiculoId != id))
            {
                return BadRequest(new { mensaje = $"La placa {model.Placa} ya pertenece a otro vehículo." });
            }

            vehiculo.Placa = model.Placa.ToUpper().Trim();
            vehiculo.Marca = model.Marca.Trim();
            vehiculo.Modelo = model.Modelo.Trim();
            vehiculo.CapacidadAsientos = model.CapacidadAsientos;
            vehiculo.CapacidadCargaKg = model.CapacidadCargaKg;
            vehiculo.SocioId = model.SocioId;
            vehiculo.SoatVencimiento = model.SoatVencimiento;
            vehiculo.RevisionTecnicaVencimiento = model.RevisionTecnicaVencimiento;
            if (model.OficinaActualId.HasValue)
            {
                vehiculo.OficinaActualId = model.OficinaActualId;
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Vehículo actualizado correctamente." });
        }

        // PATCH: api/Flota/vehiculos/{id}/estado
        [HttpPatch("vehiculos/{id}/estado")]
        public async Task<IActionResult> ToggleEstadoVehiculo(Guid id, [FromBody] CambiarEstadoDto model)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo == null)
            {
                return NotFound(new { mensaje = "El vehículo solicitado no existe." });
            }

            vehiculo.Estado = model.Estado;
            vehiculo.Activo = model.Estado != "Fuera de Servicio";

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estado del vehículo actualizado correctamente." });
        }

        #endregion

        #region --- CONDUCTORES ---

        // GET: api/Flota/conductores
        [HttpGet("conductores")]
        public async Task<IActionResult> GetConductores([FromQuery] int? oficinaId)
        {
            var query = _context.Conductores.AsQueryable();

            if (oficinaId.HasValue && oficinaId > 0)
            {
                query = query.Where(c => c.OficinaActualId == oficinaId.Value);
            }

            var conductores = await query
                .OrderByDescending(c => c.Activo)
                .ThenBy(c => c.ApellidoPaterno)
                .Select(c => new
                {
                    c.ConductorId,
                    c.NumeroDocumento,
                    NombreCompleto = $"{c.Nombres} {c.ApellidoPaterno} {c.ApellidoMaterno}",
                    c.Nombres,
                    c.ApellidoPaterno,
                    c.ApellidoMaterno,
                    c.NumeroLicencia,
                    c.CategoriaLicencia,
                    c.FechaVencimientoLicencia,
                    c.Telefono,
                    c.Activo,
                    c.OficinaActualId,
                    LicenciaPorVencer = c.FechaVencimientoLicencia <= DateOnly.FromDateTime(DateTime.Today.AddDays(30))
                })
                .ToListAsync();

            return Ok(conductores);
        }

        // POST: api/Flota/conductores
        [HttpPost("conductores")]
        public async Task<IActionResult> CreateConductor([FromBody] ConductorDto model)
        {
            if (await _context.Conductores.AnyAsync(c => c.NumeroDocumento == model.NumeroDocumento.Trim()))
            {
                return BadRequest(new { mensaje = "El número de documento ya está registrado." });
            }

            if (await _context.Conductores.AnyAsync(c => c.NumeroLicencia == model.NumeroLicencia.Trim()))
            {
                return BadRequest(new { mensaje = "El número de licencia/brevete ya está registrado." });
            }

            var conductor = new Conductore
            {
                NumeroDocumento = model.NumeroDocumento.Trim(),
                Nombres = model.Nombres.Trim(),
                ApellidoPaterno = model.ApellidoPaterno.Trim(),
                ApellidoMaterno = model.ApellidoMaterno.Trim(),
                NumeroLicencia = model.NumeroLicencia.ToUpper().Trim(),
                CategoriaLicencia = model.CategoriaLicencia.ToUpper().Trim(),
                FechaVencimientoLicencia = model.FechaVencimientoLicencia,
                Telefono = model.Telefono?.Trim(),
                OficinaActualId = model.OficinaActualId ?? 1,
                Activo = true
            };

            _context.Conductores.Add(conductor);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Conductor registrado con éxito.", conductorId = conductor.ConductorId });
        }

        // PUT: api/Flota/conductores/{id}
        [HttpPut("conductores/{id}")]
        public async Task<IActionResult> UpdateConductor(Guid id, [FromBody] ConductorDto model)
        {
            var conductor = await _context.Conductores.FindAsync(id);
            if (conductor == null)
            {
                return NotFound(new { mensaje = "El conductor solicitado no existe." });
            }

            if (await _context.Conductores.AnyAsync(c => c.NumeroDocumento == model.NumeroDocumento.Trim() && c.ConductorId != id))
            {
                return BadRequest(new { mensaje = "El DNI ingresado ya pertenece a otro conductor." });
            }

            if (await _context.Conductores.AnyAsync(c => c.NumeroLicencia == model.NumeroLicencia.Trim() && c.ConductorId != id))
            {
                return BadRequest(new { mensaje = "La licencia ingresada ya pertenece a otro conductor." });
            }

            conductor.NumeroDocumento = model.NumeroDocumento.Trim();
            conductor.Nombres = model.Nombres.Trim();
            conductor.ApellidoPaterno = model.ApellidoPaterno.Trim();
            conductor.ApellidoMaterno = model.ApellidoMaterno.Trim();
            conductor.NumeroLicencia = model.NumeroLicencia.ToUpper().Trim();
            conductor.CategoriaLicencia = model.CategoriaLicencia.ToUpper().Trim();
            conductor.FechaVencimientoLicencia = model.FechaVencimientoLicencia;
            conductor.Telefono = model.Telefono?.Trim();
            if (model.OficinaActualId.HasValue)
            {
                conductor.OficinaActualId = model.OficinaActualId;
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Conductor actualizado correctamente." });
        }

        // PATCH: api/Flota/conductores/{id}/estado
        [HttpPatch("conductores/{id}/estado")]
        public async Task<IActionResult> ToggleEstadoConductor(Guid id, [FromBody] CambiarEstadoDto model)
        {
            var conductor = await _context.Conductores.FindAsync(id);
            if (conductor == null)
            {
                return NotFound(new { mensaje = "El conductor solicitado no existe." });
            }

            conductor.Activo = model.Estado != "Inactivo";
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estado del conductor actualizado correctamente." });
        }

        #endregion

        #region --- SOCIOS INVERSIONISTAS ---

        // GET: api/Flota/socios
        [HttpGet("socios")]
        public async Task<IActionResult> GetSocios()
        {
            var socios = await _context.Socios
                .Include(s => s.Vehiculos)
                .OrderByDescending(s => s.Activo)
                .ThenBy(s => s.RazonSocialNombre)
                .Select(s => new
                {
                    s.SocioId,
                    s.RucDni,
                    s.RazonSocialNombre,
                    s.PorcentajeGanancia,
                    s.Activo,
                    CantidadVehiculos = s.Vehiculos.Count(v => v.Activo),
                    PlacasVehiculos = s.Vehiculos.Where(v => v.Activo).Select(v => v.Placa).ToList()
                })
                .ToListAsync();

            return Ok(socios);
        }

        // POST: api/Flota/socios
        [HttpPost("socios")]
        public async Task<IActionResult> CreateSocio([FromBody] SocioDto model)
        {
            if (await _context.Socios.AnyAsync(s => s.RucDni == model.RucDni.Trim()))
            {
                return BadRequest(new { mensaje = "El RUC/DNI ya se encuentra registrado." });
            }

            var socio = new Socio
            {
                RucDni = model.RucDni.Trim(),
                RazonSocialNombre = model.RazonSocialNombre.Trim(),
                PorcentajeGanancia = model.PorcentajeGanancia > 0 ? model.PorcentajeGanancia : 70.00m,
                Activo = true
            };

            _context.Socios.Add(socio);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Socio registrado con éxito.", socioId = socio.SocioId });
        }

        // PUT: api/Flota/socios/{id}
        [HttpPut("socios/{id}")]
        public async Task<IActionResult> UpdateSocio(Guid id, [FromBody] SocioDto model)
        {
            var socio = await _context.Socios.FindAsync(id);
            if (socio == null)
            {
                return NotFound(new { mensaje = "El socio solicitado no existe." });
            }

            if (await _context.Socios.AnyAsync(s => s.RucDni == model.RucDni.Trim() && s.SocioId != id))
            {
                return BadRequest(new { mensaje = "El RUC/DNI ingresado ya pertenece a otro socio." });
            }

            socio.RucDni = model.RucDni.Trim();
            socio.RazonSocialNombre = model.RazonSocialNombre.Trim();
            socio.PorcentajeGanancia = model.PorcentajeGanancia > 0 ? model.PorcentajeGanancia : 70.00m;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Socio actualizado correctamente." });
        }

        // PATCH: api/Flota/socios/{id}/estado
        [HttpPatch("socios/{id}/estado")]
        public async Task<IActionResult> ToggleEstadoSocio(Guid id, [FromBody] CambiarEstadoDto model)
        {
            var socio = await _context.Socios.FindAsync(id);
            if (socio == null)
            {
                return NotFound(new { mensaje = "El socio solicitado no existe." });
            }

            socio.Activo = model.Estado != "Inactivo";
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estado del socio actualizado correctamente." });
        }

        #endregion

        #region --- OFICINAS ---

        // GET: api/Flota/oficinas
        [HttpGet("oficinas")]
        public async Task<IActionResult> GetOficinas()
        {
            var oficinas = await _context.Oficinas
                .Where(o => o.Activo)
                .OrderBy(o => o.Nombre)
                .Select(o => new
                {
                    OficinaId = o.OficinaId,
                    Nombre = o.Nombre,
                    Direccion = o.Direccion
                })
                .ToListAsync();

            return Ok(oficinas);
        }

        #endregion
    }

    #region --- DTOs ---

    public class VehiculoDto
    {
        public string Placa { get; set; } = null!;
        public string Marca { get; set; } = null!;
        public string Modelo { get; set; } = null!;
        public int CapacidadAsientos { get; set; }
        public decimal CapacidadCargaKg { get; set; }
        public Guid? SocioId { get; set; }
        public int? OficinaActualId { get; set; }
        public DateOnly? SoatVencimiento { get; set; }
        public DateOnly? RevisionTecnicaVencimiento { get; set; }
    }

    public class ConductorDto
    {
        public string NumeroDocumento { get; set; } = null!;
        public string Nombres { get; set; } = null!;
        public string ApellidoPaterno { get; set; } = null!;
        public string ApellidoMaterno { get; set; } = null!;
        public string NumeroLicencia { get; set; } = null!;
        public string CategoriaLicencia { get; set; } = null!;
        public DateOnly FechaVencimientoLicencia { get; set; }
        public string? Telefono { get; set; }
        public int? OficinaActualId { get; set; }
    }

    public class CambiarEstadoDto
    {
        public string Estado { get; set; } = null!;
    }

    public class SocioDto
    {
        public string RucDni { get; set; } = null!;
        public string RazonSocialNombre { get; set; } = null!;
        public decimal PorcentajeGanancia { get; set; } = 70.00m;
    }

    #endregion
}