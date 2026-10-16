namespace JJR.Api.Dtos
{
    public class EncomiendaCreateDto
    {
        public int OficinaOrigenId { get; set; }
        public int OficinaDestinoId { get; set; }

        public string? ViajeId { get; set; }

        public string RemitenteDocumento { get; set; } = string.Empty;
        public string RemitenteNombres { get; set; } = string.Empty;
        public string RemitenteTelefono { get; set; } = string.Empty;

        public string DestinatarioDocumento { get; set; } = string.Empty;
        public string DestinatarioNombres { get; set; } = string.Empty;
        public string DestinatarioTelefono { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;
        public int CantidadPaquetes { get; set; } = 1;
        public decimal PesoKg { get; set; }
        public decimal PrecioEnvio { get; set; }
        public bool PorPagar { get; set; } = false;
        public string? Observaciones { get; set; }
    }

    public class EncomiendaResponseDto
    {
        public string EncomiendaId { get; set; } = string.Empty;
        public string CodigoGuia { get; set; } = string.Empty;
        public int OficinaOrigenId { get; set; }
        public string OficinaOrigenNombre { get; set; } = string.Empty;
        public int OficinaDestinoId { get; set; }
        public string OficinaDestinoNombre { get; set; } = string.Empty;

        public string? ViajeId { get; set; }
        public string? ViajeRuta { get; set; }
        public string? PlacaVehiculo { get; set; }

        public string RemitenteDocumento { get; set; } = string.Empty;
        public string RemitenteNombres { get; set; } = string.Empty;
        public string RemitenteTelefono { get; set; } = string.Empty;

        public string DestinatarioDocumento { get; set; } = string.Empty;
        public string DestinatarioNombres { get; set; } = string.Empty;
        public string DestinatarioTelefono { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;
        public int CantidadPaquetes { get; set; }
        public decimal PesoKg { get; set; }
        public decimal PrecioEnvio { get; set; }
        public bool PorPagar { get; set; }
        public string Estado { get; set; } = "REGISTRADO";
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaEntrega { get; set; }
    }

    public class CambiarEstadoEncomiendaDto
    {
        public string NuevoEstado { get; set; } = string.Empty;

        public string? ViajeId { get; set; }
        public string? Observacion { get; set; }
    }
}