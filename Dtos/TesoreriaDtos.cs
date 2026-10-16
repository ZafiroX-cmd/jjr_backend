using System.Text.Json.Serialization;

namespace JJR.Api.DTOs
{
    // --- APERTURA Y CIERRE DE CAJA ---
    public class AbrirCajaRequestDto
    {
        public Guid OficinaId { get; set; }
        public decimal MontoInicial { get; set; }
        public string? Observaciones { get; set; }
    }

    public class CerrarCajaRequestDto
    {
        public Guid CajaId { get; set; }
        public decimal MontoFisicoEfectivo { get; set; }
        public string? Observaciones { get; set; }
    }

    public class CajaEstadoResponseDto
    {
        public Guid CajaId { get; set; }
        public string OficinaNombre { get; set; } = null!;
        public string UsuarioNombre { get; set; } = null!;
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal TotalIngresosEfectivo { get; set; }
        public decimal TotalEgresosEfectivo { get; set; }
        public decimal TotalDigital { get; set; } // Yape, Plin, Transferencias
        public decimal SaldoTeoricoEfectivo => MontoInicial + TotalIngresosEfectivo - TotalEgresosEfectivo;
        public decimal? MontoFisicoEfectivo { get; set; }
        public decimal? Diferencia => MontoFisicoEfectivo.HasValue ? MontoFisicoEfectivo.Value - SaldoTeoricoEfectivo : null;
        public string Estado { get; set; } = "ABIERTA"; // ABIERTA, CERRADA
    }

    // --- MOVIMIENTOS DE CAJA ---
    public class RegistrarMovimientoRequestDto
    {
        public Guid CajaId { get; set; }
        public string TipoMovimiento { get; set; } = null!; // INGRESO, EGRESO
        public string Concepto { get; set; } = null!; // Ej: Pago de Peaje, Venta de Pasaje, Viáticos
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } = "EFECTIVO"; // EFECTIVO, YAPE, PLIN, TRANSFERENCIA
        public string? Referencia { get; set; } // Nº Operación / Boleta / Recibo
        public Guid? CuentaEmpresaId { get; set; }
    }

    public class MovimientoCajaResponseDto
    {
        public Guid MovimientoId { get; set; }
        public DateTime FechaHora { get; set; }
        public string TipoMovimiento { get; set; } = null!;
        public string Concepto { get; set; } = null!;
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } = null!;
        public string? Referencia { get; set; }
        public string UsuarioNombre { get; set; } = null!;
    }

    public class MovimientoFiltroDto
    {
        public Guid? CajaId { get; set; }
        public Guid? OficinaId { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? TipoMovimiento { get; set; }
        public string? MedioPago { get; set; }
    }
}