using System.ComponentModel.DataAnnotations;

namespace JJR.Api.Dtos
{
    public class UsuarioResponseDto
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
        public string? Dni { get; set; }
        public string? Email { get; set; }
        public string Rol { get; set; } = string.Empty;
        public int? OficinaId { get; set; }
        public string? OficinaNombre { get; set; }
        public bool Activo { get; set; }
    }

    public class CrearUsuarioDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "El username no puede exceder los 50 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string Apellidos { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Dni { get; set; }

        [EmailAddress(ErrorMessage = "El formato de correo no es válido.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(4, ErrorMessage = "La contraseña debe tener al menos 4 caracteres.")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string Rol { get; set; } = "Recepcionista";

        public int? OficinaId { get; set; }
    }

    public class ActualizarUsuarioDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string Apellidos { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Dni { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        // Opcional: Solo si se llena se actualizará la contraseña
        public string? NuevaPassword { get; set; }

        [Required]
        public string Rol { get; set; } = "Recepcionista";

        public int? OficinaId { get; set; }

        public bool Activo { get; set; } = true;
    }
}