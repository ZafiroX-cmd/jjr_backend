using JJR.Api.Dtos;
using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace JJR.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EncomiendasController : ControllerBase
    {
        private readonly JjrDbContext _context;

        public EncomiendasController(JjrDbContext context)
        {
            _context = context;
        }

        // GET: api/encomiendas
        [HttpGet]
        public async Task<IActionResult> GetEncomiendas(
            [FromQuery] int? oficinaOrigenId,
            [FromQuery] int? oficinaDestinoId,
            [FromQuery] string? estado)
        {
            var query = _context.Encomiendas
                .Include(e => e.Remitente)
                .Include(e => e.Destinatario)
                .Include(e => e.Viaje)
                    .ThenInclude(v => v!.Ruta)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(e => e.Estado == estado);
            }

            var lista = await query
                .OrderByDescending(e => e.CreadoEn)
                .Select(e => new EncomiendaResponseDto
                {
                    EncomiendaId = e.EncomiendaId.ToString(),
                    CodigoGuia = e.CodigoSeguimiento,

                    // Mapeo seguro desde Viaje -> Ruta o valores por defecto
                    OficinaOrigenId = 0,
                    OficinaOrigenNombre = e.Viaje != null && e.Viaje.Ruta != null
                        ? e.Viaje.Ruta.Origen
                        : "Agencia Origen",

                    OficinaDestinoId = 0,
                    OficinaDestinoNombre = e.Viaje != null && e.Viaje.Ruta != null
                        ? e.Viaje.Ruta.Destino
                        : "Agencia Destino",

                    ViajeId = e.ViajeId.HasValue ? e.ViajeId.Value.ToString() : null,
                    ViajeRuta = e.Viaje != null && e.Viaje.Ruta != null
                        ? $"{e.Viaje.Ruta.Origen} - {e.Viaje.Ruta.Destino}"
                        : null,
                    PlacaVehiculo = e.Viaje != null ? e.Viaje.VehiculoId.ToString() : null,

                    RemitenteDocumento = e.Remitente != null ? e.Remitente.NumeroDocumento : "",
                    RemitenteNombres = e.Remitente != null
                        ? (!string.IsNullOrWhiteSpace(e.Remitente.RazonSocial)
                            ? e.Remitente.RazonSocial
                            : $"{e.Remitente.Nombres} {e.Remitente.ApellidoPaterno} {e.Remitente.ApellidoMaterno}".Trim())
                        : "",
                    RemitenteTelefono = e.Remitente != null ? (e.Remitente.Telefono ?? "") : "",

                    DestinatarioDocumento = e.Destinatario != null ? e.Destinatario.NumeroDocumento : "",
                    DestinatarioNombres = e.Destinatario != null
                        ? (!string.IsNullOrWhiteSpace(e.Destinatario.RazonSocial)
                            ? e.Destinatario.RazonSocial
                            : $"{e.Destinatario.Nombres} {e.Destinatario.ApellidoPaterno} {e.Destinatario.ApellidoMaterno}".Trim())
                        : "",
                    DestinatarioTelefono = e.Destinatario != null ? (e.Destinatario.Telefono ?? "") : "",

                    Descripcion = e.Descripcion,
                    CantidadPaquetes = 1,
                    PesoKg = e.PesoKg,
                    PrecioEnvio = e.MontoCobro,
                    PorPagar = e.TipoCobro == "DESTINO",
                    Estado = e.Estado,
                    FechaRegistro = e.CreadoEn,
                    FechaEntrega = e.FechaEntrega
                })
                .ToListAsync();

            return Ok(lista);
        }

        // POST: api/encomiendas
        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] EncomiendaCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Buscar o crear cliente Remitente
            var remitente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.NumeroDocumento == dto.RemitenteDocumento);

            if (remitente == null)
            {
                remitente = new Cliente
                {
                    ClienteId = Guid.NewGuid(),
                    NumeroDocumento = dto.RemitenteDocumento,
                    TipoDocumento = dto.RemitenteDocumento.Length == 11 ? "RUC" : "DNI",
                    Nombres = dto.RemitenteDocumento.Length == 11 ? null : dto.RemitenteNombres,
                    RazonSocial = dto.RemitenteDocumento.Length == 11 ? dto.RemitenteNombres : null,
                    Telefono = dto.RemitenteTelefono,
                    Activo = true,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                };
                _context.Clientes.Add(remitente);
            }

            // Buscar o crear cliente Destinatario
            var destinatario = await _context.Clientes
                .FirstOrDefaultAsync(c => c.NumeroDocumento == dto.DestinatarioDocumento);

            if (destinatario == null)
            {
                destinatario = new Cliente
                {
                    ClienteId = Guid.NewGuid(),
                    NumeroDocumento = dto.DestinatarioDocumento,
                    TipoDocumento = dto.DestinatarioDocumento.Length == 11 ? "RUC" : "DNI",
                    Nombres = dto.DestinatarioDocumento.Length == 11 ? null : dto.DestinatarioNombres,
                    RazonSocial = dto.DestinatarioDocumento.Length == 11 ? dto.DestinatarioNombres : null,
                    Telefono = dto.DestinatarioTelefono,
                    Activo = true,
                    CreadoEn = DateTime.UtcNow,
                    ActualizadoEn = DateTime.UtcNow
                };
                _context.Clientes.Add(destinatario);
            }

            // Obtener el ID del usuario actual que registra
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid usuarioId = Guid.Empty;
            if (!string.IsNullOrEmpty(userIdStr)) Guid.TryParse(userIdStr, out usuarioId);

            // Generar correlativo de seguimiento
            var total = await _context.Encomiendas.CountAsync() + 1;
            var codigoGuia = $"ENC-{DateTime.Now.Year}-{total:D6}";

            Guid? viajeGuid = null;
            if (!string.IsNullOrEmpty(dto.ViajeId) && Guid.TryParse(dto.ViajeId, out var parsedViaje))
            {
                viajeGuid = parsedViaje;
            }

            var encomienda = new Encomienda
            {
                EncomiendaId = Guid.NewGuid(),
                CodigoSeguimiento = codigoGuia,
                ViajeId = viajeGuid,
                RemitenteId = remitente.ClienteId,
                DestinatarioId = destinatario.ClienteId,
                Descripcion = dto.Descripcion,
                PesoKg = dto.PesoKg,
                MontoCobro = dto.PrecioEnvio,
                TipoCobro = dto.PorPagar ? "DESTINO" : "ORIGEN",
                Estado = viajeGuid.HasValue ? "EN RUTA" : "REGISTRADO",
                ConfirmadoPorConductor = false,
                RegistradoPorUsuarioId = usuarioId,
                CreadoEn = DateTime.UtcNow
            };

            _context.Encomiendas.Add(encomienda);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Encomienda registrada correctamente", codigoGuia, encomiendaId = encomienda.EncomiendaId });
        }

        // PUT: api/encomiendas/{id}/estado
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoEncomiendaDto dto)
        {
            var enc = await _context.Encomiendas.FindAsync(id);
            if (enc == null) return NotFound("Encomienda no encontrada.");

            enc.Estado = dto.NuevoEstado;

            if (!string.IsNullOrEmpty(dto.ViajeId) && Guid.TryParse(dto.ViajeId, out var viajeGuid))
            {
                enc.ViajeId = viajeGuid;
            }

            if (dto.NuevoEstado == "ENTREGADO")
            {
                enc.FechaEntrega = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = $"Estado actualizado a {dto.NuevoEstado}" });
        }
    }
}