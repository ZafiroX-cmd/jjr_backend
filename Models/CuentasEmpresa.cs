using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("cuentas_empresa")]
public partial class CuentasEmpresa
{
    [Key]
    [Column("cuenta_id")]
    public Guid CuentaId { get; set; }

    [Column("nombre_cuenta")]
    [StringLength(100)]
    public string NombreCuenta { get; set; } = null!;

    [Column("tipo_cuenta")]
    [StringLength(30)]
    public string TipoCuenta { get; set; } = null!;

    [Column("banco_nombre")]
    [StringLength(50)]
    public string? BancoNombre { get; set; }

    [Column("numero_cuenta")]
    [StringLength(50)]
    public string? NumeroCuenta { get; set; }

    [Column("saldo_actual")]
    [Precision(12, 2)]
    public decimal SaldoActual { get; set; }

    [Column("moneda")]
    [StringLength(3)]
    public string Moneda { get; set; } = null!;

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [InverseProperty("Cuenta")]
    public virtual ICollection<MovimientosCaja> MovimientosCajas { get; set; } = new List<MovimientosCaja>();
}
