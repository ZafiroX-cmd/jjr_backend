using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("usuarios")]
[Index("Username", Name = "idx_usuarios_username")]
[Index("Dni", Name = "usuarios_dni_key", IsUnique = true)]
[Index("Username", Name = "usuarios_username_key", IsUnique = true)]
public partial class Usuario
{
    [Key]
    [Column("usuario_id")]
    public Guid UsuarioId { get; set; }

    [Column("rol_id")]
    public int RolId { get; set; }

    [Column("oficina_id")]
    public int? OficinaId { get; set; }

    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Column("dni")]
    [StringLength(15)]
    public string? Dni { get; set; }

    [Column("nombres")]
    [StringLength(100)]
    public string Nombres { get; set; } = null!;

    [Column("apellidos")]
    [StringLength(100)]
    public string Apellidos { get; set; } = null!;

    [Column("email")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Column("telefono")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [InverseProperty("Usuario")]
    public virtual ICollection<ComprobantesPago> ComprobantesPagos { get; set; } = new List<ComprobantesPago>();

    [InverseProperty("Usuario")]
    public virtual Conductore? Conductore { get; set; }

    [InverseProperty("Usuario")]
    public virtual ICollection<ContratosLaborale> ContratosLaborales { get; set; } = new List<ContratosLaborale>();

    [InverseProperty("RegistradoPorUsuario")]
    public virtual ICollection<EgresosOperativo> EgresosOperativos { get; set; } = new List<EgresosOperativo>();

    [InverseProperty("RegistradoPorUsuario")]
    public virtual ICollection<Encomienda> Encomienda { get; set; } = new List<Encomienda>();

    [InverseProperty("UsuarioRegistro")]
    public virtual ICollection<MovimientosCaja> MovimientosCajas { get; set; } = new List<MovimientosCaja>();

    [InverseProperty("EjecutadoPorUsuario")]
    public virtual ICollection<PagosNomina> PagosNominas { get; set; } = new List<PagosNomina>();

    [InverseProperty("CreadoPorUsuario")]
    public virtual ICollection<Pasaje> Pasajes { get; set; } = new List<Pasaje>();

    [ForeignKey("RolId")]
    [InverseProperty("Usuarios")]
    public virtual Role Rol { get; set; } = null!;

    [ForeignKey("OficinaId")]
    [InverseProperty("Usuarios")]
    public virtual Oficina? Oficina { get; set; }

    [InverseProperty("Usuario")]
    public virtual Socio? Socio { get; set; }
}
