using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("encomiendas")]
[Index("CodigoSeguimiento", Name = "encomiendas_codigo_seguimiento_key", IsUnique = true)]
[Index("MovilLocalId", Name = "idx_encomiendas_offline")]
[Index("ViajeId", Name = "idx_encomiendas_viaje")]
public partial class Encomienda
{
    [Key]
    [Column("encomienda_id")]
    public Guid EncomiendaId { get; set; }

    [Column("codigo_seguimiento")]
    [StringLength(20)]
    public string CodigoSeguimiento { get; set; } = null!;


    [Column("viaje_id")]
    public Guid? ViajeId { get; set; }

    [Column("remitente_id")]
    public Guid RemitenteId { get; set; }

    [Column("destinatario_id")]
    public Guid DestinatarioId { get; set; }

    [Column("descripcion")]
    public string Descripcion { get; set; } = null!;

    [Column("peso_kg")]
    [Precision(6, 2)]
    public decimal PesoKg { get; set; }

    [Column("monto_cobro")]
    [Precision(10, 2)]
    public decimal MontoCobro { get; set; }

    [Column("tipo_cobro")]
    [StringLength(20)]
    public string TipoCobro { get; set; } = null!;

    [Column("estado")]
    [StringLength(30)]
    public string Estado { get; set; } = null!;

    [Column("confirmado_por_conductor")]
    public bool ConfirmadoPorConductor { get; set; }

    [Column("fecha_entrega")]
    public DateTime? FechaEntrega { get; set; }

    [Column("movil_local_id")]
    [StringLength(50)]
    public string? MovilLocalId { get; set; }

    [Column("sincronizado_en")]
    public DateTime? SincronizadoEn { get; set; }

    [Column("registrado_por_usuario_id")]
    public Guid RegistradoPorUsuarioId { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }



    [ForeignKey("DestinatarioId")]
    [InverseProperty("EncomiendaDestinatarios")]
    public virtual Cliente Destinatario { get; set; } = null!;

    [ForeignKey("RegistradoPorUsuarioId")]
    [InverseProperty("Encomienda")]
    public virtual Usuario RegistradoPorUsuario { get; set; } = null!;

    [ForeignKey("RemitenteId")]
    [InverseProperty("EncomiendaRemitentes")]
    public virtual Cliente Remitente { get; set; } = null!;

    [ForeignKey("ViajeId")]
    [InverseProperty("Encomienda")]
    public virtual Viaje? Viaje { get; set; }

    [InverseProperty("Encomienda")]
    public virtual ICollection<ComprobantesPago> ComprobantesPagos { get; set; } = new List<ComprobantesPago>();
}