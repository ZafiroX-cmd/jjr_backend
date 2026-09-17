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

        // DTO de respuesta estructurado exactamente como lo requiere React
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
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { mensaje = "Ingresa tu usuario y contraseña para continuar." });
            }

            // 1. Buscar usuario por username e incluir la tabla Rol
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Username == request.Username.Trim() && u.Activo);

            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            // 2. Verificar contraseña mediante BCrypt o fallback para clave directa de desarrollo
            bool esPasswordValido = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);

            if (!esPasswordValido && request.Password == usuario.PasswordHash)
            {
                esPasswordValido = true;
            }

            if (!esPasswordValido)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            // 3. Generar JWT Token
            var token = GenerarJwtToken(usuario);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = usuario.Username,
                NombreCompleto = $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
                Rol = usuario.Rol.Nombre,
                UsuarioId = usuario.UsuarioId
            });
        }

        private string GenerarJwtToken(Usuario usuario)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraYLargaJJR2026!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim(ClaimTypes.Role, usuario.Rol.Nombre)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // Duración por turno laboral
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}