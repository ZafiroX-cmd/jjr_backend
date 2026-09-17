using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("rutas")]
public partial class Ruta
{
    [Key]
    [Column("ruta_id")]
    public int RutaId { get; set; }

    [Column("origen")]
    [StringLength(100)]
    public string Origen { get; set; } = null!;

    [Column("destino")]
    [StringLength(100)]
    public string Destino { get; set; } = null!;

    [Column("distancia_km")]
    [Precision(6, 2)]
    public decimal? DistanciaKm { get; set; }

    [Column("precio_pasaje_base")]
    [Precision(10, 2)]
    public decimal PrecioPasajeBase { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [InverseProperty("Ruta")]
    public virtual ICollection<PredichosDemandum> PredichosDemanda { get; set; } = new List<PredichosDemandum>();

    [InverseProperty("Ruta")]
    public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();

    [InverseProperty("Ruta")]
    public virtual ICollection<RutaParada> Paradas { get; set; } = new List<RutaParada>();
}
