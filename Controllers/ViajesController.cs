using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JJR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ViajesController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public ViajesController(JjrDbContext context)
        {
            _context = context;
        }

        #region --- GESTIÓN DE VIAJES (SALIDAS) ---

        // GET: api/Viajes
        [HttpGet]
        public async Task<IActionResult> GetViajes([FromQuery] string? estado, [FromQuery] int? oficinaId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var userOficinaClaim = User.FindFirst("OficinaId")?.Value;

            var query = _context.Viajes
                .Include(v => v.Ruta)
                .Include(v => v.Vehiculo)
                .Include(v => v.Conductor)
                .Include(v => v.Pasajes)
                .Include(v => v.OficinaOrigen)
                .AsQueryable();

            bool esAdminOGerente = userRole.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                                  userRole.Equals("Gerente", StringComparison.OrdinalIgnoreCase);

            if (esAdminOGerente)
            {
                if (oficinaId.HasValue && oficinaId > 0)
                {
                    query = query.Where(v => v.OficinaOrigenId == oficinaId.Value);
                }
            }
            else
            {
                if (int.TryParse(userOficinaClaim, out int userOficinaId))
                {
                    query = query.Where(v => v.OficinaOrigenId == userOficinaId);
                }
            }

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                query = query.Where(v => v.Estado == estado);
            }

            var viajes = await query
                .OrderByDescending(v => v.FechaSalida)
                .Select(v => new
                {
                    v.ViajeId,
                    RutaNombre = v.Ruta.Origen + " - " + v.Ruta.Destino,
                    RutaId = v.RutaId,
                    OficinaOrigenId = v.OficinaOrigenId,
                    OficinaOrigenNombre = v.OficinaOrigen != null ? v.OficinaOrigen.Nombre : "No asignado",
                    VehiculoId = v.VehiculoId,
                    Placa = v.Vehiculo.Placa,
                    ConductorId = v.ConductorId,
                    ConductorNombre = v.Conductor.Nombres + " " + v.Conductor.ApellidoPaterno + " " + v.Conductor.ApellidoMaterno,
                    FechaHoraSalida = v.FechaSalida,
                    PrecioPasaje = v.PrecioPasaje,
                    Estado = v.Estado,
                    ManifiestoCerrado = v.ManifiestoCerrado,
                    AsientosOcupados = v.Pasajes.Count(p => p.Estado != "Anulado"),
                    AsientosDisponibles = 4 - v.Pasajes.Count(p => p.Estado != "Anulado")
                })
                .ToListAsync();

            return Ok(viajes);
        }

        // POST: api/Viajes
        [HttpPost]
        public async Task<IActionResult> CreateViaje([FromBody] ViajeCreateDto model)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var userOficinaClaim = User.FindFirst("OficinaId")?.Value;

            int oficinaOrigenIdFinal;

            bool esAdminOGerente = userRole.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                                  userRole.Equals("Gerente", StringComparison.OrdinalIgnoreCase);

            if (esAdminOGerente)
            {
                if (model.OficinaOrigenId <= 0)
                {
                    return BadRequest(new { mensaje = "Como administrador o gerente debe seleccionar una oficina de origen válida." });
                }
                oficinaOrigenIdFinal = model.OficinaOrigenId;
            }
            else
            {
                if (string.IsNullOrEmpty(userOficinaClaim) || !int.TryParse(userOficinaClaim, out int userOficinaId))
                {
                    return BadRequest(new { mensaje = "El usuario recepcionista no tiene una oficina asignada en su perfil." });
                }
                oficinaOrigenIdFinal = userOficinaId;
            }

            // 1. Validar que el vehículo exista y esté físicamente en la oficina de origen
            var vehiculoObj = await _context.Vehiculos.FindAsync(model.VehiculoId);
            if (vehiculoObj == null)
            {
                return BadRequest(new { mensaje = "El vehículo seleccionado no existe." });
            }

            if (vehiculoObj.OficinaActualId.HasValue && vehiculoObj.OficinaActualId.Value != oficinaOrigenIdFinal)
            {
                var oficinaActual = await _context.Oficinas.FindAsync(vehiculoObj.OficinaActualId.Value);
                string nombreOficinaActual = oficinaActual?.Nombre ?? $"Oficina #{vehiculoObj.OficinaActualId}";
                return BadRequest(new { mensaje = $"La camioneta {vehiculoObj.Placa} no se encuentra en la oficina de origen seleccionada. Actualmente está en {nombreOficinaActual}." });
            }

            // 2. Validar que el conductor exista y esté físicamente en la oficina de origen
            var conductorObj = await _context.Conductores.FindAsync(model.ConductorId);
            if (conductorObj == null)
            {
                return BadRequest(new { mensaje = "El conductor seleccionado no existe." });
            }

            if (conductorObj.OficinaActualId.HasValue && conductorObj.OficinaActualId.Value != oficinaOrigenIdFinal)
            {
                return BadRequest(new { mensaje = $"El conductor {conductorObj.Nombres} {conductorObj.ApellidoPaterno} no se encuentra en la oficina de origen de este viaje." });
            }

            // 3. Validar que la camioneta no esté ocupada en otro viaje activo
            var vehiculoOcupado = await _context.Viajes.AnyAsync(v =>
                v.VehiculoId == model.VehiculoId &&
                (v.Estado == "Programado" || v.Estado == "En Ruta"));

            if (vehiculoOcupado)
            {
                return BadRequest(new { mensaje = "La camioneta seleccionada ya tiene un viaje activo o programado." });
            }

            // 4. Validar que el conductor no esté asignado a otro viaje activo
            var conductorOcupado = await _context.Viajes.AnyAsync(v =>
                v.ConductorId == model.ConductorId &&
                (v.Estado == "Programado" || v.Estado == "En Ruta"));

            if (conductorOcupado)
            {
                return BadRequest(new { mensaje = "El conductor seleccionado ya se encuentra asignado a un viaje en curso." });
            }

            var nuevoViaje = new Viaje
            {
                RutaId = model.RutaId,
                VehiculoId = model.VehiculoId,
                ConductorId = model.ConductorId,
                OficinaOrigenId = oficinaOrigenIdFinal,
                FechaSalida = model.FechaSalida.ToUniversalTime(),
                PrecioPasaje = model.PrecioPasaje,
                Estado = "Programado",
                ManifiestoCerrado = false,
                CreadoEn = DateTime.UtcNow
            };

            _context.Viajes.Add(nuevoViaje);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Salida programada exitosamente.", viajeId = nuevoViaje.ViajeId });
        }

        // PATCH: api/Viajes/{id}/estado
        [HttpPatch("{id}/estado")]
        public async Task<IActionResult> UpdateEstadoViaje(Guid id, [FromBody] UpdateEstadoDto model)
        {
            var viaje = await _context.Viajes
                .Include(v => v.Ruta)
                .Include(v => v.Vehiculo)
                .Include(v => v.Conductor)
                .FirstOrDefaultAsync(v => v.ViajeId == id);

            if (viaje == null) return NotFound(new { mensaje = "Viaje no encontrado." });

            string[] estadosPermitidos = { "Programado", "En Ruta", "Finalizado", "Cancelado" };
            if (!estadosPermitidos.Contains(model.Estado))
            {
                return BadRequest(new { mensaje = "Estado de viaje no válido." });
            }

            viaje.Estado = model.Estado;

            if (model.Estado == "En Ruta")
            {
                if (viaje.Vehiculo != null) viaje.Vehiculo.OficinaActualId = null;
                if (viaje.Conductor != null) viaje.Conductor.OficinaActualId = null;
            }
            else if (model.Estado == "Finalizado")
            {
                viaje.ManifiestoCerrado = true;
                viaje.FechaLlegadaEstimada = DateTime.UtcNow;

                var destinoStr = viaje.Ruta.Destino.Trim().ToLower();
                var oficinaDestino = await _context.Oficinas
                    .FirstOrDefaultAsync(o => o.Nombre.ToLower() == destinoStr);

                if (oficinaDestino != null)
                {
                    if (viaje.Vehiculo != null) viaje.Vehiculo.OficinaActualId = oficinaDestino.OficinaId;
                    if (viaje.Conductor != null) viaje.Conductor.OficinaActualId = oficinaDestino.OficinaId;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"El viaje ha sido actualizado a estado: {model.Estado}." });
        }

        #endregion

        #region --- ENDPOINTS AUXILIARES ---

        // GET: api/Viajes/recursos?oficinaOrigenId=1
        [HttpGet("recursos")]
        public async Task<IActionResult> GetRecursos([FromQuery] int? oficinaOrigenId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            int? oficinaUsuarioClaim = int.TryParse(User.FindFirst("OficinaId")?.Value, out int id) ? id : null;

            bool esAdminOGerente = userRole.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                                  userRole.Equals("Gerente", StringComparison.OrdinalIgnoreCase);

            // Determinar la oficina que se usará para filtrar vehículos, conductores y salidas
            int? oficinaFiltro = oficinaOrigenId;
            if (!esAdminOGerente || !oficinaFiltro.HasValue)
            {
                oficinaFiltro = oficinaFiltro ?? oficinaUsuarioClaim;
            }

            var rutas = await _context.Rutas
                .Where(r => r.Activo)
                .Select(r => new
                {
                    r.RutaId,
                    r.Origen,
                    r.Destino,
                    Nombre = r.Origen + " - " + r.Destino,
                    PrecioSugerido = r.PrecioPasajeBase,
                    Paradas = _context.RutaParadas
                        .Where(p => p.RutaId == r.RutaId && p.Activo)
                        .OrderBy(p => p.Orden)
                        .Select(p => new
                        {
                            p.ParadaId,
                            p.NombreParada,
                            p.Orden,
                            p.EsOficina
                        })
                        .ToList()
                })
                .ToListAsync();

            // Consulta de Vehículos: Se exige que coincida con la oficina requerida
            var queryVehiculos = _context.Vehiculos
                .Where(v => v.Activo && (v.Estado == "Operativo" || v.Estado == "Disponible"));

            if (oficinaFiltro.HasValue && oficinaFiltro.Value > 0)
            {
                queryVehiculos = queryVehiculos.Where(v => v.OficinaActualId == oficinaFiltro.Value || v.OficinaActualId == null);
            }

            var vehiculos = await queryVehiculos
                .Select(v => new { v.VehiculoId, v.Placa, v.Marca, v.OficinaActualId })
                .ToListAsync();

            // Consulta de Conductores: Se exige que coincida con la oficina requerida
            var queryConductores = _context.Conductores.Where(c => c.Activo);

            if (oficinaFiltro.HasValue && oficinaFiltro.Value > 0)
            {
                queryConductores = queryConductores.Where(c => c.OficinaActualId == oficinaFiltro.Value || c.OficinaActualId == null);
            }

            var conductores = await queryConductores
                .Select(c => new
                {
                    c.ConductorId,
                    Nombre = c.Nombres + " " + c.ApellidoPaterno + " " + c.ApellidoMaterno,
                    c.OficinaActualId
                })
                .ToListAsync();

            var oficinas = await _context.Oficinas
                .Where(o => o.Activo)
                .Select(o => new { o.OficinaId, o.Nombre, o.Direccion })
                .ToListAsync();

            // 🚀 OBTENER SALIDAS/VIAJES PROGRAMADOS ACTIVOS
            var queryViajes = _context.Viajes
                .Include(v => v.Ruta)
                .Include(v => v.Vehiculo)
                .Include(v => v.Conductor)
                .Where(v => v.Estado == "Programado" || v.Estado == "En Ruta");

            if (oficinaFiltro.HasValue && oficinaFiltro.Value > 0)
            {
                queryViajes = queryViajes.Where(v => v.OficinaOrigenId == oficinaFiltro.Value);
            }

            var viajes = await queryViajes
                .OrderBy(v => v.FechaSalida)
                .Select(v => new
                {
                    viajeId = v.ViajeId,
                    fechaHoraSalida = v.FechaSalida.ToString("dd/MM/yyyy HH:mm"),
                    origenNombre = v.Ruta.Origen,
                    destinoNombre = v.Ruta.Destino,
                    vehiculoPlaca = v.Vehiculo.Placa,
                    conductorNombre = v.Conductor != null ? v.Conductor.Nombres + " " + v.Conductor.ApellidoPaterno : ""
                })
                .ToListAsync();

            // Retornamos todas las entidades incluyendo la propiedad 'viajes'
            return Ok(new { rutas, vehiculos, conductores, oficinas, viajes });
        }

        #endregion
    }

    #region --- DTOs ---

    public class ViajeCreateDto
    {
        public int RutaId { get; set; }
        public Guid VehiculoId { get; set; }
        public Guid ConductorId { get; set; }
        public int OficinaOrigenId { get; set; }
        public DateTime FechaSalida { get; set; }
        public decimal PrecioPasaje { get; set; }
    }

    public class UpdateEstadoDto
    {
        public string Estado { get; set; } = null!;
    }

    #endregion
}