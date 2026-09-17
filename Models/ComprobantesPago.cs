using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("comprobantes_pago")]
[Index("ClienteNumeroDeDocumento", Name = "idx_comprobantes_cliente")]
[Index("EstadoSunat", Name = "idx_comprobantes_estado")]
[Index("FechaDeEmision", Name = "idx_comprobantes_fecha")]
[Index("Serie", "Numero", Name = "uq_serie_numero", IsUnique = true)]
public partial class ComprobantesPago
{
    [Key]
    [Column("comprobante_id")]
    public Guid ComprobanteId { get; set; }

    [Column("documento")]
    [StringLength(20)]
    public string Documento { get; set; } = null!;

    [Column("serie")]
    [StringLength(10)]
    public string Serie { get; set; } = null!;

    [Column("numero")]
    public int Numero { get; set; }

    [Column("fecha_de_emision")]
    public DateOnly FechaDeEmision { get; set; }

    [Column("hora_de_emision")]
    public TimeOnly? HoraDeEmision { get; set; }

    [Column("observacion")]
    public string? Observacion { get; set; }

    [Column("moneda")]
    [StringLength(5)]
    public string Moneda { get; set; } = null!;

    [Column("tipo_operacion")]
    [StringLength(10)]
    public string TipoOperacion { get; set; } = null!;

    [Column("cliente_id")]
    public Guid ClienteId { get; set; }

    [Column("cliente_tipo_de_documento")]
    [StringLength(5)]
    public string ClienteTipoDeDocumento { get; set; } = null!;

    [Column("cliente_numero_de_documento")]
    [StringLength(20)]
    public string ClienteNumeroDeDocumento { get; set; } = null!;

    [Column("cliente_denominacion")]
    [StringLength(200)]
    public string ClienteDenominacion { get; set; } = null!;

    [Column("cliente_direccion")]
    [StringLength(250)]
    public string? ClienteDireccion { get; set; }

    [Column("monto_gravado")]
    [Precision(12, 2)]
    public decimal MontoGravado { get; set; }

    [Column("monto_igv")]
    [Precision(12, 2)]
    public decimal MontoIgv { get; set; }

    [Column("total")]
    [Precision(12, 2)]
    public decimal Total { get; set; }

    [Column("estado_sunat")]
    [StringLength(30)]
    public string EstadoSunat { get; set; } = null!;

    [Column("hash_cdr")]
    public string? HashCdr { get; set; }

    [Column("respuesta_sunat")]
    public string? RespuestaSunat { get; set; }

    [Column("pdf_url")]
    public string? PdfUrl { get; set; }

    [Column("xml_url")]
    public string? XmlUrl { get; set; }

    [Column("pasaje_id")]
    public Guid? PasajeId { get; set; }

    [Column("encomienda_id")]
    public Guid? EncomiendaId { get; set; }

    [Column("usuario_id")]
    public Guid UsuarioId { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [ForeignKey("ClienteId")]
    [InverseProperty("ComprobantesPagos")]
    public virtual Cliente Cliente { get; set; } = null!;

    [ForeignKey("EncomiendaId")]
    [InverseProperty("ComprobantesPagos")]
    public virtual Encomienda? Encomienda { get; set; }

    [InverseProperty("Comprobante")]
    public virtual ICollection<ItemsComprobante> ItemsComprobantes { get; set; } = new List<ItemsComprobante>();

    [ForeignKey("PasajeId")]
    [InverseProperty("ComprobantesPagos")]
    public virtual Pasaje? Pasaje { get; set; }

    [ForeignKey("UsuarioId")]
    [InverseProperty("ComprobantesPagos")]
    public virtual Usuario Usuario { get; set; } = null!;
}
