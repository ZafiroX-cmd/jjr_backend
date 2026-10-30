using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("movimientos_caja")]
[Index("CuentaId", Name = "idx_movimientos_cuenta")]
public partial class MovimientosCaja
{
    [Key]
    [Column("movimiento_id")]
    public Guid MovimientoId { get; set; }

    [Column("cuenta_id")]
    public Guid CuentaId { get; set; }

    [Column("tipo_movimiento")]
    [StringLength(20)]
    public string TipoMovimiento { get; set; } = "INGRESO";

    [Column("monto")]
    [Precision(10, 2)]
    public decimal Monto { get; set; }

    [Column("tipo_movimiento")]
    public TipoMovimientoCaja TipoMovimiento { get; set; }

    [Column("categoria")]
    [StringLength(50)]
    public string Categoria { get; set; } = null!;

    [Column("pago_nomina_id")]
    public Guid? PagoNominaId { get; set; }

    [Column("socio_id")]
    public Guid? SocioId { get; set; }

    [Column("usuario_registro_id")]
    public Guid UsuarioRegistroId { get; set; }

    [Column("descripcion")]
    public string Descripcion { get; set; } = null!;

    [Column("fecha_movimiento")]
    public DateTime FechaMovimiento { get; set; }

    [ForeignKey("CuentaId")]
    [InverseProperty("MovimientosCajas")]
    public virtual CuentasEmpresa Cuenta { get; set; } = null!;

    [ForeignKey("PagoNominaId")]
    [InverseProperty("MovimientosCajas")]
    public virtual PagosNomina? PagoNomina { get; set; }

    [ForeignKey("SocioId")]
    [InverseProperty("MovimientosCajas")]
    public virtual Socio? Socio { get; set; }

    [ForeignKey("UsuarioRegistroId")]
    [InverseProperty("MovimientosCajas")]
    public virtual Usuario UsuarioRegistro { get; set; } = null!;
}