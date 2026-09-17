using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("socios")]
[Index("RucDni", Name = "socios_ruc_dni_key", IsUnique = true)]
[Index("UsuarioId", Name = "socios_usuario_id_key", IsUnique = true)]
public partial class Socio
{
    [Key]
    [Column("socio_id")]
    public Guid SocioId { get; set; }

    [Column("usuario_id")]
    public Guid? UsuarioId { get; set; }

    [Column("ruc_dni")]
    [StringLength(20)]
    public string RucDni { get; set; } = null!;

    [Column("razon_social_nombre")]
    [StringLength(200)]
    public string RazonSocialNombre { get; set; } = null!;

    [Column("porcentaje_ganancia")]
    [Precision(5, 2)]
    public decimal PorcentajeGanancia { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [InverseProperty("Socio")]
    public virtual ICollection<ContratosLaborale> ContratosLaborales { get; set; } = new List<ContratosLaborale>();

    [InverseProperty("Socio")]
    public virtual ICollection<LiquidacionesSocio> LiquidacionesSocios { get; set; } = new List<LiquidacionesSocio>();

    [InverseProperty("Socio")]
    public virtual ICollection<MovimientosCaja> MovimientosCajas { get; set; } = new List<MovimientosCaja>();

    [ForeignKey("UsuarioId")]
    [InverseProperty("Socio")]
    public virtual Usuario? Usuario { get; set; }

    [InverseProperty("Socio")]
    public virtual ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}
