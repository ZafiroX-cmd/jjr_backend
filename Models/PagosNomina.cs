using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("pagos_nomina")]
[Index("ContratoId", "PeriodoMes", "PeriodoAnio", Name = "uq_pago_periodo", IsUnique = true)]
public partial class PagosNomina
{
    [Key]
    [Column("pago_id")]
    public Guid PagoId { get; set; }

    [Column("contrato_id")]
    public Guid ContratoId { get; set; }

    [Column("periodo_mes")]
    public int PeriodoMes { get; set; }

    [Column("periodo_anio")]
    public int PeriodoAnio { get; set; }

    [Column("sueldo_ganado")]
    [Precision(10, 2)]
    public decimal SueldoGanado { get; set; }

    [Column("total_descuentos")]
    [Precision(10, 2)]
    public decimal? TotalDescuentos { get; set; }

    [Column("total_adelantos")]
    [Precision(10, 2)]
    public decimal? TotalAdelantos { get; set; }

    [Column("monto_neto_pagar")]
    [Precision(10, 2)]
    public decimal MontoNetoPagar { get; set; }

    [Column("fecha_pago")]
    public DateTime? FechaPago { get; set; }

    [Column("ejecutado_por_usuario_id")]
    public Guid? EjecutadoPorUsuarioId { get; set; }

    [Column("observacion_pago")]
    public string? ObservacionPago { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("ContratoId")]
    [InverseProperty("PagosNominas")]
    public virtual ContratosLaborale Contrato { get; set; } = null!;

    [ForeignKey("EjecutadoPorUsuarioId")]
    [InverseProperty("PagosNominas")]
    public virtual Usuario? EjecutadoPorUsuario { get; set; }

    [InverseProperty("PagoNomina")]
    public virtual ICollection<MovimientosCaja> MovimientosCajas { get; set; } = new List<MovimientosCaja>();
}
