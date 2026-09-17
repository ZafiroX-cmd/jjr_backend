using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("dias_festivos")]
public partial class DiasFestivo
{
    [Key]
    [Column("fecha")]
    public DateOnly Fecha { get; set; }

    [Column("descripcion")]
    [StringLength(100)]
    public string Descripcion { get; set; } = null!;

    [Column("es_alta_demanda")]
    public bool EsAltaDemanda { get; set; }
}
