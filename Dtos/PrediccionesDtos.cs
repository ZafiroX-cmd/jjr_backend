namespace JJR.Api.Dtos
{
    public class PrediccionDemandaRutaDto
    {
        public string Ruta { get; set; } = null!;
        public string DiaSemana { get; set; } = null!;
        public int AsientosOcupadosPromedio { get; set; }
        public decimal NivelDemandaPorcentaje { get; set; }
        public string Recomendacion { get; set; } = null!;
    }

    public class ProyeccionFlujoCajaDto
    {
        public string Mes { get; set; } = null!;
        public decimal IngresosProyectados { get; set; }
        public decimal EgresosProyectados { get; set; }
        public decimal UtilidadEstimada { get; set; }
    }

    public class DashboardIAResponseDto
    {
        public List<PrediccionDemandaRutaDto> DemandaRutas { get; set; } = new();
        public List<ProyeccionFlujoCajaDto> ProyeccionFinanciera { get; set; } = new();
        public int TotalViajesAnalizados { get; set; }
        public decimal PrecisionModelo { get; set; } = 95.0m;
    }
}