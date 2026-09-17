using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("viajes")]
[Index("FechaSalida", "RutaId", Name = "idx_viajes_fecha_ruta")]
public partial class Viaje
{
    [Key]
    [Column("viaje_id")]
    public Guid ViajeId { get; set; }

    [Column("ruta_id")]
    public int RutaId { get; set; }

    [Column("vehiculo_id")]
    public Guid VehiculoId { get; set; }

    [Column("conductor_id")]
    public Guid ConductorId { get; set; }

    [Column("oficina_origen_id")]
    public int? OficinaOrigenId { get; set; }

    [Column("fecha_salida")]
    public DateTime FechaSalida { get; set; }

    [Column("fecha_llegada_estimada")]
    public DateTime? FechaLlegadaEstimada { get; set; }

    [Column("precio_pasaje")]
    [Precision(10, 2)]
    public decimal PrecioPasaje { get; set; }

    [Column("estado")]
    [StringLength(30)]
    public string? Estado { get; set; }

    [Column("manifiesto_cerrado")]
    public bool ManifiestoCerrado { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("OficinaOrigenId")]
    [InverseProperty("ViajesOrigen")]
    public virtual Oficina? OficinaOrigen { get; set; }

    [ForeignKey("ConductorId")]
    [InverseProperty("Viajes")]
    public virtual Conductore Conductor { get; set; } = null!;

    [InverseProperty("Viaje")]
    public virtual ICollection<EgresosOperativo> EgresosOperativos { get; set; } = new List<EgresosOperativo>();

    [InverseProperty("Viaje")]
    public virtual ICollection<Encomienda> Encomienda { get; set; } = new List<Encomienda>();

    [InverseProperty("Viaje")]
    public virtual LiquidacionesSocio? LiquidacionesSocio { get; set; }

    [InverseProperty("Viaje")]
    public virtual ICollection<Pasaje> Pasajes { get; set; } = new List<Pasaje>();

    [ForeignKey("RutaId")]
    [InverseProperty("Viajes")]
    public virtual Ruta Ruta { get; set; } = null!;

    [ForeignKey("VehiculoId")]
    [InverseProperty("Viajes")]
    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
