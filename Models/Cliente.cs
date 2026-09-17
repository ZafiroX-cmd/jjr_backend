using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("clientes")]
[Index("NumeroDocumento", Name = "clientes_numero_documento_key", IsUnique = true)]
[Index("NumeroDocumento", Name = "idx_clientes_documento")]
public partial class Cliente
{
    [Key]
    [Column("cliente_id")]
    public Guid ClienteId { get; set; }

    [Column("numero_documento")]
    [StringLength(15)]
    public string NumeroDocumento { get; set; } = null!;

    [Column("tipo_documento")]
    [StringLength(10)]
    public string? TipoDocumento { get; set; } = null!;

    [Column("nombres")]
    [StringLength(100)]
    public string? Nombres { get; set; }

    [Column("apellido_paterno")]
    [StringLength(80)]
    public string? ApellidoPaterno { get; set; }

    [Column("apellido_materno")]
    [StringLength(80)]
    public string? ApellidoMaterno { get; set; }

    [Column("razon_social")]
    [StringLength(200)]
    public string? RazonSocial { get; set; }

    [Column("nombre_comercial")]
    [StringLength(150)]
    public string? NombreComercial { get; set; }

    [Column("direccion_fiscal")]
    public string? DireccionFiscal { get; set; }

    [Column("ubigeo")]
    [StringLength(6)]
    public string? Ubigeo { get; set; }

    [Column("telefono")]
    [StringLength(20)]
    public string? Telefono { get; set; }

    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [Column("actualizado_en")]
    public DateTime ActualizadoEn { get; set; }

    [InverseProperty("Cliente")]
    public virtual ICollection<ComprobantesPago> ComprobantesPagos { get; set; } = new List<ComprobantesPago>();

    [InverseProperty("Destinatario")]
    public virtual ICollection<Encomienda> EncomiendaDestinatarios { get; set; } = new List<Encomienda>();

    [InverseProperty("Remitente")]
    public virtual ICollection<Encomienda> EncomiendaRemitentes { get; set; } = new List<Encomienda>();

    [InverseProperty("ClienteFacturacion")]
    public virtual ICollection<Pasaje> PasajeClienteFacturacions { get; set; } = new List<Pasaje>();

    [InverseProperty("ClientePasajero")]
    public virtual ICollection<Pasaje> PasajeClientePasajeros { get; set; } = new List<Pasaje>();
}
