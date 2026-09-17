using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("items_comprobante")]
public partial class ItemsComprobante
{
    [Key]
    [Column("item_id")]
    public Guid ItemId { get; set; }

    [Column("comprobante_id")]
    public Guid ComprobanteId { get; set; }

    [Column("unidad_de_medida")]
    [StringLength(10)]
    public string UnidadDeMedida { get; set; } = null!;

    [Column("codigo_interno")]
    [StringLength(50)]
    public string? CodigoInterno { get; set; }

    [Column("descripcion")]
    [StringLength(250)]
    public string Descripcion { get; set; } = null!;

    [Column("cantidad")]
    [Precision(10, 2)]
    public decimal Cantidad { get; set; }

    [Column("valor_unitario")]
    [Precision(16, 6)]
    public decimal ValorUnitario { get; set; }

    [Column("porcentaje_igv")]
    [Precision(5, 2)]
    public decimal PorcentajeIgv { get; set; }

    [Column("codigo_tipo_afectacion_igv")]
    [StringLength(5)]
    public string CodigoTipoAfectacionIgv { get; set; } = null!;

    [Column("nombre_tributo")]
    [StringLength(10)]
    public string NombreTributo { get; set; } = null!;

    [ForeignKey("ComprobanteId")]
    [InverseProperty("ItemsComprobantes")]
    public virtual ComprobantesPago Comprobante { get; set; } = null!;
}
