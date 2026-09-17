using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("series_comprobantes")]
[Index("Serie", Name = "series_comprobantes_serie_key", IsUnique = true)]
public partial class SeriesComprobante
{
    [Key]
    [Column("serie_id")]
    public int SerieId { get; set; }

    [Column("tipo_documento")]
    [StringLength(20)]
    public string TipoDocumento { get; set; } = null!;

    [Column("serie")]
    [StringLength(10)]
    public string Serie { get; set; } = null!;

    [Column("ultimo_numero")]
    public int UltimoNumero { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }
}
