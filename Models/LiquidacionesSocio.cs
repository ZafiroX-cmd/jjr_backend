using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("liquidaciones_socios")]
[Index("ViajeId", Name = "liquidaciones_socios_viaje_id_key", IsUnique = true)]
public partial class LiquidacionesSocio
{
    [Key]
    [Column("liquidacion_id")]
    public Guid LiquidacionId { get; set; }

    [Column("socio_id")]
    public Guid SocioId { get; set; }

    [Column("viaje_id")]
    public Guid ViajeId { get; set; }

    [Column("monto_ingreso_pasajes")]
    [Precision(10, 2)]
    public decimal? MontoIngresoPasajes { get; set; }

    [Column("monto_ingreso_encomiendas")]
    [Precision(10, 2)]
    public decimal? MontoIngresoEncomiendas { get; set; }

    [Column("monto_egresos_deducibles")]
    [Precision(10, 2)]
    public decimal? MontoEgresosDeducibles { get; set; }

    [Column("monto_bruto_total")]
    [Precision(10, 2)]
    public decimal MontoBrutoTotal { get; set; }

    [Column("porcentaje_socio")]
    [Precision(5, 2)]
    public decimal PorcentajeSocio { get; set; }

    [Column("monto_ganancia_socio")]
    [Precision(10, 2)]
    public decimal MontoGananciaSocio { get; set; }

    [Column("monto_ganancia_empresa")]
    [Precision(10, 2)]
    public decimal MontoGananciaEmpresa { get; set; }

    [Column("estado_pago")]
    [StringLength(30)]
    public string EstadoPago { get; set; } = null!;

    [Column("fecha_pago")]
    public DateTime? FechaPago { get; set; }

    [Column("procesado_en")]
    public DateTime ProcesadoEn { get; set; }

    [ForeignKey("SocioId")]
    [InverseProperty("LiquidacionesSocios")]
    public virtual Socio Socio { get; set; } = null!;

    [ForeignKey("ViajeId")]
    [InverseProperty("LiquidacionesSocio")]
    public virtual Viaje Viaje { get; set; } = null!;
}
