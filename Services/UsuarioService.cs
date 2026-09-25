using Microsoft.EntityFrameworkCore;
using JJR.Api.Dtos;
using JJR.Api.Models;

namespace JJR.Api.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly JjrDbContext _context;

        public UsuarioService(JjrDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UsuarioResponseDto>> ObtenerTodosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Oficina)
                .AsNoTracking()
                .Select(u => new UsuarioResponseDto
                {
                    UsuarioId = u.UsuarioId.ToString(),
                    Username = u.Username,
                    Nombres = u.Nombres,
                    Apellidos = u.Apellidos,
                    Dni = u.Dni,
                    Email = u.Email,
                    Rol = u.Rol.Nombre,
                    OficinaId = u.OficinaId,
                    OficinaNombre = u.Oficina != null ? u.Oficina.Nombre : null,
                    Activo = u.Activo
                })
                .ToListAsync();
        }

        public async Task<(bool Exito, string Mensaje, string? UsuarioId)> CrearAsync(CrearUsuarioDto dto)
        {
            var usernameNormalizado = dto.Username.Trim().ToLower();

            // Validar username único
            var existeUsername = await _context.Usuarios
                .AnyAsync(u => u.Username.ToLower() == usernameNormalizado);

            if (existeUsername)
            {
                return (false, "El nombre de usuario ya se encuentra registrado.", null);
            }

            // Validar DNI único opcionalmente si se proporciona
            if (!string.IsNullOrWhiteSpace(dto.Dni))
            {
                var existeDni = await _context.Usuarios.AnyAsync(u => u.Dni == dto.Dni.Trim());
                if (existeDni)
                {
                    return (false, "El DNI ingresado ya pertenece a otro usuario.", null);
                }
            }

            // Validar oficina si es Recepcionista
            if (dto.Rol == "Recepcionista" && (!dto.OficinaId.HasValue || dto.OficinaId <= 0))
            {
                return (false, "Es obligatorio asignar una oficina al personal con rol Recepcionista.", null);
            }

            // Buscar la entidad Rol
            var rolEntity = await _context.Roles
                .FirstOrDefaultAsync(r => r.Nombre.ToLower() == dto.Rol.Trim().ToLower());

            if (rolEntity == null)
            {
                return (false, $"El rol '{dto.Rol}' no existe en el sistema.", null);
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash);
            var nuevoUsuarioId = Guid.NewGuid();

            var nuevoUsuario = new Usuario
            {
                UsuarioId = nuevoUsuarioId,
                Username = usernameNormalizado,
                Nombres = dto.Nombres.Trim(),
                Apellidos = dto.Apellidos.Trim(),
                Dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLower(),
                PasswordHash = passwordHash,
                RolId = rolEntity.RolId,
                OficinaId = dto.Rol == "Administrador" ? null : dto.OficinaId,
                Activo = true,
                CreadoEn = DateTime.UtcNow
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return (true, "Usuario registrado exitosamente.", nuevoUsuarioId.ToString());
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarAsync(string usuarioId, ActualizarUsuarioDto dto)
        {
            if (!Guid.TryParse(usuarioId, out var idGuid))
            {
                return (false, "El ID de usuario proporcionado no es válido.");
            }

            var usuario = await _context.Usuarios.FindAsync(idGuid);
            if (usuario == null)
            {
                return (false, "El usuario especificado no existe.");
            }

            var usernameNormalizado = dto.Username.Trim().ToLower();

            // Validar que el username no lo esté usando otro usuario
            var existeUsername = await _context.Usuarios
                .AnyAsync(u => u.UsuarioId != idGuid && u.Username.ToLower() == usernameNormalizado);

            if (existeUsername)
            {
                return (false, "El nombre de usuario ya está registrado por otra cuenta.");
            }

            // Validar DNI único si fue modificado
            if (!string.IsNullOrWhiteSpace(dto.Dni))
            {
                var existeDni = await _context.Usuarios
                    .AnyAsync(u => u.UsuarioId != idGuid && u.Dni == dto.Dni.Trim());
                if (existeDni)
                {
                    return (false, "El DNI ingresado pertenece a otro usuario.");
                }
            }

            // Validar asignación de oficina
            if (dto.Rol == "Recepcionista" && (!dto.OficinaId.HasValue || dto.OficinaId <= 0))
            {
                return (false, "Es obligatorio asignar una oficina al personal con rol Recepcionista.");
            }

            // Buscar la entidad del nuevo rol
            var rolEntity = await _context.Roles
                .FirstOrDefaultAsync(r => r.Nombre.ToLower() == dto.Rol.Trim().ToLower());

            if (rolEntity == null)
            {
                return (false, $"El rol '{dto.Rol}' no existe en el sistema.");
            }

            // Actualización de campos
            usuario.Username = usernameNormalizado;
            usuario.Nombres = dto.Nombres.Trim();
            usuario.Apellidos = dto.Apellidos.Trim();
            usuario.Dni = string.IsNullOrWhiteSpace(dto.Dni) ? null : dto.Dni.Trim();
            usuario.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLower();
            usuario.RolId = rolEntity.RolId;
            usuario.OficinaId = dto.Rol == "Administrador" ? null : dto.OficinaId;
            usuario.Activo = dto.Activo;

            // Actualizar contraseña solo si se especificó un valor nuevo
            if (!string.IsNullOrWhiteSpace(dto.NuevaPassword))
            {
                if (dto.NuevaPassword.Trim().Length < 4)
                {
                    return (false, "La nueva contraseña debe tener al menos 4 caracteres.");
                }
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword.Trim());
            }

            await _context.SaveChangesAsync();
            return (true, "Usuario actualizado correctamente.");
        }
    }
}