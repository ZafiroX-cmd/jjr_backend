namespace JJR.Api.Dtos
{
    public class ApiSunatDocumentDto
    {
        public string documento { get; set; } = null!;
        public string serie { get; set; } = null!;
        public int numero { get; set; }
        public string fecha_de_emision { get; set; } = null!;
        public string hora_de_emision { get; set; } = null!;
        public string moneda { get; set; } = "PEN";
        public string tipo_operacion { get; set; } = "0101";
        public string cliente_tipo_de_documento { get; set; } = null!;
        public string cliente_numero_de_documento { get; set; } = null!;
        public string cliente_denominacion { get; set; } = null!;
        public string cliente_direccion { get; set; } = "TRUJILLO";
        public List<ApiSunatItemDto> items { get; set; } = new();
        public string total { get; set; } = null!;
    }

    public class ApiSunatItemDto
    {
        public string unidad_de_medida { get; set; } = "ZZ";
        public string codigo_interno { get; set; } = null!;
        public string descripcion { get; set; } = null!;
        public string cantidad { get; set; } = "1";
        public string valor_unitario { get; set; } = null!;
        public string porcentaje_igv { get; set; } = "18";
        public string codigo_tipo_afectacion_igv { get; set; } = "10";
        public string nombre_tributo { get; set; } = "IGV";
    }

    public class ApiSunatResponseDto
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public string? file_pdf { get; set; }
        public string? file_xml { get; set; }
        public string? file_cdr { get; set; }
    }
}