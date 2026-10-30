namespace JJR.Api.Dtos
{
    public class PreviaLiquidacionViajeDto
    {
        public Guid ViajeId { get; set; }
        public Guid SocioId { get; set; }
        public string NombreSocio { get; set; } = null!;
        public string PlacaVehiculo { get; set; } = null!;
        public string RutaOrigenDestino { get; set; } = null!;
        public DateTime FechaSalida { get; set; }
        public decimal MontoIngresoPasajes { get; set; }
        public decimal MontoIngresoEncomiendas { get; set; }
        public decimal MontoEgresosDeducibles { get; set; }
        public decimal MontoBrutoTotal { get; set; }
        public decimal PorcentajeSocio { get; set; }
        public decimal MontoGananciaSocio { get; set; }
        public decimal MontoGananciaEmpresa { get; set; }
        public bool YaLiquidado { get; set; }
    }

    public class ProcesarLiquidacionRequestDto
    {
        public Guid ViajeId { get; set; }
        public Guid CuentaId { get; set; } 
    }

    public class LiquidacionSocioResponseDto
    {
        public Guid LiquidacionId { get; set; }
        public Guid ViajeId { get; set; }
        public string NombreSocio { get; set; } = null!;
        public string PlacaVehiculo { get; set; } = null!;
        public string RutaOrigenDestino { get; set; } = null!;
        public decimal MontoBrutoTotal { get; set; }
        public decimal PorcentajeSocio { get; set; }
        public decimal MontoGananciaSocio { get; set; }
        public decimal MontoGananciaEmpresa { get; set; }
        public string EstadoPago { get; set; } = null!;
        public DateTime? FechaPago { get; set; }
        public DateTime ProcesadoEn { get; set; }
    }
}