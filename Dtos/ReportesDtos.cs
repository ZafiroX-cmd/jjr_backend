namespace JJR.Api.Dtos
{
    public class DesgloseCategoriaDto
    {
        public string Categoria { get; set; } = null!;
        public decimal Monto { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class ReporteFinancieroResponseDto
    {
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal UtilidadNeta { get; set; }
        public int TotalPasajesVendidos { get; set; }
        public int TotalEncomiendasProcesadas { get; set; }
        public List<DesgloseCategoriaDto> IngresosPorCategoria { get; set; } = new();
        public List<DesgloseCategoriaDto> EgresosPorCategoria { get; set; } = new();
    }
}