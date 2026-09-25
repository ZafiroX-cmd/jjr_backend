using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using JJR.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JJR.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JjrDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(JjrDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // DTO de petición de Login
        public class LoginRequest
        {
            public string Username { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        // DTO de respuesta estructurado para React
        public class LoginResponse
        {
            [JsonPropertyName("token")]
            public string Token { get; set; } = null!;

            [JsonPropertyName("username")]
            public string Username { get; set; } = null!;

            [JsonPropertyName("nombreCompleto")]
            public string NombreCompleto { get; set; } = null!;

            [JsonPropertyName("rol")]
            public string Rol { get; set; } = null!;

            [JsonPropertyName("usuarioId")]
            public Guid UsuarioId { get; set; }

            [JsonPropertyName("oficinaId")]
            public int? OficinaId { get; set; }

            [JsonPropertyName("oficinaNombre")]
            public string? OficinaNombre { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { mensaje = "Ingresa tu usuario y contraseña para continuar." });
            }

            // 1. Buscar usuario e incluir la tabla Rol y la tabla Oficina (CRUCIAL)
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Oficina)
                .FirstOrDefaultAsync(u => u.Username == request.Username.Trim() && u.Activo);

            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            // 2. Verificar contraseña mediante BCrypt o fallback para clave directa
            bool esPasswordValido = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);

            if (!esPasswordValido && request.Password == usuario.PasswordHash)
            {
                esPasswordValido = true;
            }

            if (!esPasswordValido)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            // 3. Generar JWT Token con las claims actualizadas (incluye OficinaId)
            var token = GenerarJwtToken(usuario);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = usuario.Username,
                NombreCompleto = $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
                Rol = usuario.Rol.Nombre,
                UsuarioId = usuario.UsuarioId,
                OficinaId = usuario.OficinaId,
                OficinaNombre = usuario.Oficina?.Nombre
            });
        }

        private string GenerarJwtToken(Usuario usuario)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraYLargaJJR2026!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim(ClaimTypes.Role, usuario.Rol.Nombre)
            };

            // AGREGAR CLAIM DE OFICINA ID SI EXISTE
            if (usuario.OficinaId.HasValue)
            {
                claims.Add(new Claim("OficinaId", usuario.OficinaId.Value.ToString()));
                claims.Add(new Claim("oficina_id", usuario.OficinaId.Value.ToString())); // Alias común
            }

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // Duración por turno laboral
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}