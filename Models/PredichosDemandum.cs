using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("predichos_demanda")]
public partial class PredichosDemandum
{
    [Key]
    [Column("prediccion_id")]
    public Guid PrediccionId { get; set; }

    [Column("ruta_id")]
    public int RutaId { get; set; }

    [Column("fecha_proyectada")]
    public DateOnly FechaProyectada { get; set; }

    [Column("pasajeros_estimados")]
    public int PasajerosEstimados { get; set; }

    [Column("carga_estimada_kg")]
    [Precision(8, 2)]
    public decimal CargaEstimadaKg { get; set; }

    [Column("vehiculos_sugeridos")]
    public int VehiculosSugeridos { get; set; }

    [Column("confianza_modelo")]
    [Precision(5, 2)]
    public decimal? ConfianzaModelo { get; set; }

    [Column("generado_en")]
    public DateTime GeneradoEn { get; set; }

    [ForeignKey("RutaId")]
    [InverseProperty("PredichosDemanda")]
    public virtual Ruta Ruta { get; set; } = null!;
}
