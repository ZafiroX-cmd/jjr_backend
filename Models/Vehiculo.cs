using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("vehiculos")]
[Index("Placa", Name = "vehiculos_placa_key", IsUnique = true)]
public partial class Vehiculo
{
    [Key]
    [Column("vehiculo_id")]
    public Guid VehiculoId { get; set; }

    [Column("socio_id")]
    public Guid? SocioId { get; set; }

    [Column("placa")]
    [StringLength(10)]
    public string Placa { get; set; } = null!;

    [Column("marca")]
    [StringLength(50)]
    public string Marca { get; set; } = null!;

    [Column("modelo")]
    [StringLength(50)]
    public string Modelo { get; set; } = null!;

    [Column("capacidad_asientos")]
    public int CapacidadAsientos { get; set; }

    [Column("capacidad_carga_kg")]
    [Precision(8, 2)]
    public decimal CapacidadCargaKg { get; set; }

    [Column("soat_vencimiento")]
    public DateOnly? SoatVencimiento { get; set; }

    [Column("revision_tecnica_vencimiento")]
    public DateOnly? RevisionTecnicaVencimiento { get; set; }

    [Column("estado")]
    [StringLength(30)]
    public string? Estado { get; set; }

    [Column("oficina_actual_id")]
    public int? OficinaActualId { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [InverseProperty("Vehiculo")]
    public virtual ICollection<EgresosOperativo> EgresosOperativos { get; set; } = new List<EgresosOperativo>();

    [ForeignKey("SocioId")]
    [InverseProperty("Vehiculos")]
    public virtual Socio? Socio { get; set; }

    [ForeignKey("OficinaActualId")]
    public virtual Oficina? OficinaActual { get; set; }

    [InverseProperty("Vehiculo")]
    public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
}
