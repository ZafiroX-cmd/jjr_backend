using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("adelantos_descuentos")]
public partial class AdelantosDescuento
{
    [Key]
    [Column("movimiento_id")]
    public Guid MovimientoId { get; set; }

    [Column("contrato_id")]
    public Guid ContratoId { get; set; }

    [Column("tipo")]
    [StringLength(20)]
    public string Tipo { get; set; } = null!;

    [Column("monto")]
    [Precision(10, 2)]
    public decimal Monto { get; set; }

    [Column("motivo")]
    public string Motivo { get; set; } = null!;

    [Column("fecha_registro")]
    public DateOnly FechaRegistro { get; set; }

    [Column("procesado_en_nomina")]
    public bool ProcesadoEnNomina { get; set; }

    [ForeignKey("ContratoId")]
    [InverseProperty("AdelantosDescuentos")]
    public virtual ContratosLaborale Contrato { get; set; } = null!;
}
