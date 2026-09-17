using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JJR.Api.Models
{
    [Table("oficinas")]
    public partial class Oficina
    {
        [Key]
        [Column("oficina_id")]
        public int OficinaId { get; set; }

        [Column("nombre")]
        [StringLength(50)]
        public string Nombre { get; set; } = null!;

        [Column("ubigeo")]
        [StringLength(6)]
        public string? Ubigeo { get; set; }

        [Column("direccion")]
        [StringLength(150)]
        public string? Direccion { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("creado_en")]
        public DateTime? CreadoEn { get; set; }

        [InverseProperty("Oficina")]
        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

        [InverseProperty("OficinaOrigen")]
        public virtual ICollection<Viaje> ViajesOrigen { get; set; } = new List<Viaje>();

        [InverseProperty("OficinaEmision")]
        public virtual ICollection<Pasaje> PasajesEmitidos { get; set; } = new List<Pasaje>();
    }
}