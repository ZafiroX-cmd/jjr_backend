using System.Text.Json.Serialization;

namespace JJR.Api.DTOs
{
    public class ComprobanteFiltroDto
    {
        public string? TipoComprobante { get; set; } // BOLETA, FACTURA, NOTA_DE_VENTA
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? Busqueda { get; set; } // Serie, Correlativo, DNI/RUC o Cliente
    }

    public class ComprobanteResponseDto
    {
        [JsonPropertyName("comprobanteId")]
        public int ComprobanteId { get; set; }

        [JsonPropertyName("tipoComprobante")]
        public string TipoComprobante { get; set; } = null!; // BOLETA, FACTURA, NOTA_DE_VENTA

        [JsonPropertyName("serie")]
        public string Serie { get; set; } = null!; // B001, F001, NV01

        [JsonPropertyName("correlativo")]
        public string Correlativo { get; set; } = null!; // 00000001

        [JsonPropertyName("numeroCompleto")]
        public string NumeroCompleto => $"{Serie}-{Correlativo}";

        [JsonPropertyName("fechaEmision")]
        public DateTime FechaEmision { get; set; }

        [JsonPropertyName("clienteDocumento")]
        public string ClienteDocumento { get; set; } = null!;

        [JsonPropertyName("clienteNombres")]
        public string ClienteNombres { get; set; } = null!;

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("igv")]
        public decimal Igv { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("estadoSunat")]
        public string EstadoSunat { get; set; } = "PENDIENTE"; // PENDIENTE, ACEPTADO, RECHAZADO, ANULADO

        [JsonPropertyName("mensajeSunat")]
        public string? MensajeSunat { get; set; }

        [JsonPropertyName("encomiendaId")]
        public int? EncomiendaId { get; set; }

        [JsonPropertyName("oficinaNombre")]
        public string? OficinaNombre { get; set; }
    }

    public class AnularComprobanteRequestDto
    {
        public string Motivo { get; set; } = null!;
    }

    public class ArchivoComprobanteDto
    {
        public byte[] ContenidoBytes { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/pdf";
        public string NombreArchivo { get; set; } = "comprobante.pdf";
    }
}