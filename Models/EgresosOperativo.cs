using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("egresos_operativos")]
[Index("ViajeId", Name = "idx_egresos_viaje")]
public partial class EgresosOperativo
{
    [Key]
    [Column("egreso_id")]
    public Guid EgresoId { get; set; }

    [Column("viaje_id")]
    public Guid? ViajeId { get; set; }

    [Column("vehiculo_id")]
    public Guid? VehiculoId { get; set; }

    [Column("categoria")]
    [StringLength(50)]
    public string Categoria { get; set; } = null!;

    [Column("monto")]
    [Precision(10, 2)]
    public decimal Monto { get; set; }

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("url_foto_comprobante")]
    [StringLength(500)]
    public string? UrlFotoComprobante { get; set; }

    [Column("validado_por_admin")]
    public bool ValidadoPorAdmin { get; set; }

    [Column("registrado_por_usuario_id")]
    public Guid RegistradoPorUsuarioId { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("RegistradoPorUsuarioId")]
    [InverseProperty("EgresosOperativos")]
    public virtual Usuario RegistradoPorUsuario { get; set; } = null!;

    [ForeignKey("VehiculoId")]
    [InverseProperty("EgresosOperativos")]
    public virtual Vehiculo? Vehiculo { get; set; }

    [ForeignKey("ViajeId")]
    [InverseProperty("EgresosOperativos")]
    public virtual Viaje? Viaje { get; set; }
}
