using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("pasajes")]
[Index("ViajeId", "Estado", Name = "idx_pasajes_viaje_estado")]
[Index("ViajeId", "NumeroAsiento", Name = "uq_viaje_asiento", IsUnique = true)]
public partial class Pasaje
{
    [Key]
    [Column("pasaje_id")]
    public Guid PasajeId { get; set; }

    [Column("viaje_id")]
    public Guid ViajeId { get; set; }

    [Column("numero_asiento")]
    public int NumeroAsiento { get; set; }

    [Column("cliente_pasajero_id")]
    public Guid ClientePasajeroId { get; set; }

    [Column("cliente_facturacion_id")]
    public Guid? ClienteFacturacionId { get; set; }

    [Column("precio")]
    [Precision(10, 2)]
    public decimal Precio { get; set; }

    [Column("estado")]
    [StringLength(30)]
    public string Estado { get; set; } = null!;

    [Column("bloqueado_hasta")]
    public DateTime? BloqueadoHasta { get; set; }

    [Column("comprobante_emitido")]
    public bool ComprobanteEmitido { get; set; }

    [Column("creado_por_usuario_id")]
    public Guid CreadoPorUsuarioId { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [Column("oficina_emision_id")]
    public int? OficinaEmisionId { get; set; }

    [Column("origen_parada_id")]
    public int? OrigenParadaId { get; set; }

    [Column("destino_parada_id")]
    public int? DestinoParadaId { get; set; }

    // --- NUEVAS COLUMNAS FINANCIERAS Y COMPROBANTES ---

    [Column("tipo_operacion")]
    [StringLength(20)]
    public string TipoOperacion { get; set; } = "VENTA_DIRECTA";

    [Column("monto_adelanto")]
    [Precision(10, 2)]
    public decimal MontoAdelanto { get; set; } = 0.00m;

    [Column("medio_pago")]
    [StringLength(30)]
    public string MedioPago { get; set; } = "EFECTIVO";

    [Column("comprobante_pago_url")]
    [StringLength(255)]
    public string? ComprobantePagoUrl { get; set; }

    [Column("numero_operacion")]
    [StringLength(50)]
    public string? NumeroOperacion { get; set; }


    [ForeignKey("ClienteFacturacionId")]
    [InverseProperty("PasajeClienteFacturacions")]
    public virtual Cliente? ClienteFacturacion { get; set; }

    [ForeignKey("ClientePasajeroId")]
    [InverseProperty("PasajeClientePasajeros")]
    public virtual Cliente ClientePasajero { get; set; } = null!;

    [InverseProperty("Pasaje")]
    public virtual ICollection<ComprobantesPago> ComprobantesPagos { get; set; } = new List<ComprobantesPago>();

    [ForeignKey("CreadoPorUsuarioId")]
    [InverseProperty("Pasajes")]
    public virtual Usuario CreadoPorUsuario { get; set; } = null!;

    [ForeignKey("ViajeId")]
    [InverseProperty("Pasajes")]
    public virtual Viaje Viaje { get; set; } = null!;

    [ForeignKey("OficinaEmisionId")]
    public virtual Oficina? OficinaEmision { get; set; }

    [ForeignKey("OrigenParadaId")]
    public virtual RutaParada? OrigenParada { get; set; }

    [ForeignKey("DestinoParadaId")]
    public virtual RutaParada? DestinoParada { get; set; }
}