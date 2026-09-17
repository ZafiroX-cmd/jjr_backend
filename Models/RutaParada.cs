using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JJR.Api.Models
{
    [Table("ruta_paradas")]
    public partial class RutaParada
    {
        [Key]
        [Column("parada_id")]
        public int ParadaId { get; set; }

        [Column("ruta_id")]
        public int RutaId { get; set; }

        [Column("nombre_parada")]
        [StringLength(100)]
        public string NombreParada { get; set; } = null!;

        [Column("orden")]
        public int Orden { get; set; }

        [Column("es_oficina")]
        public bool EsOficina { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [ForeignKey("RutaId")]
        [InverseProperty("Paradas")]
        public virtual Ruta Ruta { get; set; } = null!;
    }
}