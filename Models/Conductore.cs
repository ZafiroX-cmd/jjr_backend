using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("conductores")]
[Index("NumeroDocumento", Name = "conductores_numero_documento_key", IsUnique = true)]
[Index("NumeroLicencia", Name = "conductores_numero_licencia_key", IsUnique = true)]
[Index("UsuarioId", Name = "conductores_usuario_id_key", IsUnique = true)]
public partial class Conductore
{
    [Key]
    [Column("conductor_id")]
    public Guid ConductorId { get; set; }

    [Column("usuario_id")]
    public Guid? UsuarioId { get; set; }

    [Column("numero_documento")]
    [StringLength(15)]
    public string NumeroDocumento { get; set; } = null!;

    [Column("nombres")]
    [StringLength(100)]
    public string Nombres { get; set; } = null!;

    [Column("apellido_paterno")]
    [StringLength(80)]
    public string ApellidoPaterno { get; set; } = null!;

    [Column("apellido_materno")]
    [StringLength(80)]
    public string ApellidoMaterno { get; set; } = null!;

    [Column("numero_licencia")]
    [StringLength(20)]
    public string NumeroLicencia { get; set; } = null!;

    [Column("categoria_licencia")]
    [StringLength(10)]
    public string CategoriaLicencia { get; set; } = null!;

    [Column("fecha_vencimiento_licencia")]
    public DateOnly FechaVencimientoLicencia { get; set; }

    [Column("telefono")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    [Column("oficina_actual_id")]
    public int? OficinaActualId { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [InverseProperty("Conductor")]
    public virtual ICollection<ContratosLaborale> ContratosLaborales { get; set; } = new List<ContratosLaborale>();

    [ForeignKey("UsuarioId")]
    [InverseProperty("Conductore")]
    public virtual Usuario? Usuario { get; set; }

    [ForeignKey("OficinaActualId")]
    public virtual Oficina? OficinaActual { get; set; }

    [InverseProperty("Conductor")]
    public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
}
