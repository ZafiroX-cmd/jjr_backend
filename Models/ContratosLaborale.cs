using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Models;

[Table("contratos_laborales")]
public partial class ContratosLaborale
{
    [Key]
    [Column("contrato_id")]
    public Guid ContratoId { get; set; }

    [Column("usuario_id")]
    public Guid? UsuarioId { get; set; }

    [Column("conductor_id")]
    public Guid? ConductorId { get; set; }

    [Column("socio_id")]
    public Guid? SocioId { get; set; }

    [Column("sueldo_base")]
    [Precision(10, 2)]
    public decimal SueldoBase { get; set; }

    [Column("pago_por_viaje")]
    [Precision(10, 2)]
    public decimal? PagoPorViaje { get; set; }

    [Column("fecha_inicio")]
    public DateOnly FechaInicio { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("creado_en")]
    public DateTime CreadoEn { get; set; }

    [Column("tipo_empleado")]
    [StringLength(50)]
    public string TipoEmpleado { get; set; } = null!;

    [InverseProperty("Contrato")]
    public virtual ICollection<AdelantosDescuento> AdelantosDescuentos { get; set; } = new List<AdelantosDescuento>();

    [ForeignKey("ConductorId")]
    [InverseProperty("ContratosLaborales")]
    public virtual Conductore? Conductor { get; set; }

    [InverseProperty("Contrato")]
    public virtual ICollection<PagosNomina> PagosNominas { get; set; } = new List<PagosNomina>();

    [ForeignKey("SocioId")]
    [InverseProperty("ContratosLaborales")]
    public virtual Socio? Socio { get; set; }

    [ForeignKey("UsuarioId")]
    [InverseProperty("ContratosLaborales")]
    public virtual Usuario? Usuario { get; set; }
}
