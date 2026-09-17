using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JJR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT
    public class ClientesController : ControllerBase
    {
        private readonly JjrDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ClientesController(JjrDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        #region --- CONSULTA INTELIGENTE DNI/RUC (DECOLECTA) ---

        [HttpGet("consulta/{numero}")]
        public async Task<IActionResult> ConsultarDocumento(string numero)
        {
            numero = numero.Trim();
            if (numero.Length != 8 && numero.Length != 11)
            {
                return BadRequest(new { mensaje = "El número debe tener 8 (DNI) u 11 (RUC) dígitos." });
            }

            // 1. Buscar en la Base de Datos Local primero
            var clienteLocal = await _context.Clientes.FirstOrDefaultAsync(c => c.NumeroDocumento == numero);

            if (clienteLocal != null)
            {
                return Ok(new
                {
                    fuente = "LOCAL",
                    numeroDocumento = clienteLocal.NumeroDocumento,
                    nombres = clienteLocal.Nombres,
                    apellidoPaterno = clienteLocal.ApellidoPaterno,
                    apellidoMaterno = clienteLocal.ApellidoMaterno,
                    razonSocial = clienteLocal.RazonSocial,
                    nombreComercial = clienteLocal.NombreComercial,
                    direccionFiscal = clienteLocal.DireccionFiscal,
                    ubigeo = clienteLocal.Ubigeo,
                    telefono = clienteLocal.Telefono,
                    email = clienteLocal.Email
                });
            }

            // 2. Si no existe localmente, consultar a Decolecta (API RENIEC/SUNAT)
            var token = _configuration["DecolectaApi:Token"];
            var baseUrl = _configuration["DecolectaApi:BaseUrl"];

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(baseUrl))
            {
                return StatusCode(500, new { mensaje = "La API Key de Decolecta no está configurada en el servidor." });
            }

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                if (numero.Length == 8) // DNI
                {
                    var response = await client.GetAsync($"{baseUrl}/v1/reniec/dni?numero={numero}");
                    if (!response.IsSuccessStatusCode) return NotFound(new { mensaje = "DNI no encontrado en RENIEC." });

                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    return Ok(new
                    {
                        fuente = "RENIEC",
                        numeroDocumento = GetStringSafe(data, "document_number"),
                        nombres = GetStringSafe(data, "first_name"),
                        apellidoPaterno = GetStringSafe(data, "first_last_name"),
                        apellidoMaterno = GetStringSafe(data, "second_last_name")
                    });
                }
                else // RUC
                {
                    var response = await client.GetAsync($"{baseUrl}/v1/sunat/ruc?numero={numero}");
                    if (!response.IsSuccessStatusCode) return NotFound(new { mensaje = "RUC no encontrado en SUNAT." });

                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    return Ok(new
                    {
                        fuente = "SUNAT",
                        numeroDocumento = GetStringSafe(data, "numero_documento"),
                        razonSocial = GetStringSafe(data, "razon_social"),
                        direccionFiscal = GetStringSafe(data, "direccion"),
                        ubigeo = GetStringSafe(data, "ubigeo"),
                        estado = GetStringSafe(data, "estado"),
                        condicion = GetStringSafe(data, "condicion")
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al conectar con los servicios de validación.", detalle = ex.Message });
            }
        }

        // Helper para leer el JSON sin que explote si un campo viene null o no existe
        private string? GetStringSafe(JsonElement element, string propName)
        {
            if (element.TryGetProperty(propName, out var prop) && prop.ValueKind != JsonValueKind.Null)
            {
                return prop.GetString();
            }
            return null;
        }

        #endregion

        #region --- GESTIÓN DE CLIENTES ---

        // GET: api/Clientes
        [HttpGet]
        public async Task<IActionResult> GetClientes()
        {
            var clientes = await _context.Clientes
                .OrderByDescending(c => c.CreadoEn)
                .Select(c => new
                {
                    c.ClienteId,
                    c.NumeroDocumento,
                    
                    NombreCliente = c.NumeroDocumento.Length == 8
                        ? $"{c.Nombres} {c.ApellidoPaterno} {c.ApellidoMaterno}".Trim()
                        : c.RazonSocial,
                    c.DireccionFiscal,
                    c.Telefono,
                    c.Email,
                    c.Activo
                })
                .ToListAsync();

            return Ok(clientes);
        }

        // POST: api/Clientes
        [HttpPost]
        public async Task<IActionResult> CreateCliente([FromBody] ClienteDto model)
        {
            var docLimpio = model.NumeroDocumento.Trim();

            if (await _context.Clientes.AnyAsync(c => c.NumeroDocumento == docLimpio))
            {
                return BadRequest(new { mensaje = "El documento ya está registrado en el sistema." });
            }

            // Deducimos el tipo de documento según la cantidad de dígitos (8 = DNI, 11 = RUC)
            string tipoDoc = docLimpio.Length == 11 ? "RUC" : "DNI";

            var cliente = new Cliente
            {
                NumeroDocumento = docLimpio,
                TipoDocumento = tipoDoc, 
                Nombres = model.Nombres?.Trim().ToUpper(),
                ApellidoPaterno = model.ApellidoPaterno?.Trim().ToUpper(),
                ApellidoMaterno = model.ApellidoMaterno?.Trim().ToUpper(),
                RazonSocial = model.RazonSocial?.Trim().ToUpper(),
                NombreComercial = model.NombreComercial?.Trim().ToUpper(),
                DireccionFiscal = model.DireccionFiscal?.Trim().ToUpper(),
                Ubigeo = model.Ubigeo?.Trim(),
                Telefono = model.Telefono?.Trim(),
                Email = model.Email?.Trim().ToLower(),
                Activo = true,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Cliente registrado exitosamente.", clienteId = cliente.ClienteId });
        }

        // PUT: api/Clientes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCliente(Guid id, [FromBody] ClienteDto model)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound(new { mensaje = "El cliente no existe." });

            cliente.Nombres = model.Nombres?.Trim().ToUpper();
            cliente.ApellidoPaterno = model.ApellidoPaterno?.Trim().ToUpper();
            cliente.ApellidoMaterno = model.ApellidoMaterno?.Trim().ToUpper();
            cliente.RazonSocial = model.RazonSocial?.Trim().ToUpper();
            cliente.NombreComercial = model.NombreComercial?.Trim().ToUpper();
            cliente.DireccionFiscal = model.DireccionFiscal?.Trim().ToUpper();
            cliente.Ubigeo = model.Ubigeo?.Trim();
            cliente.Telefono = model.Telefono?.Trim();
            cliente.Email = model.Email?.Trim().ToLower();
            cliente.ActualizadoEn = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Cliente actualizado correctamente." });
        }

        #endregion
    }

    #region --- DTOs ---

    public class ClienteDto
    {
        public string NumeroDocumento { get; set; } = null!;
        public string? Nombres { get; set; }
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }
        public string? RazonSocial { get; set; }
        public string? NombreComercial { get; set; }
        public string? DireccionFiscal { get; set; }
        public string? Ubigeo { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }

    #endregion
}