namespace JJR.Api.DTOs
{
    public class CuentaEmpresaDto
    {
        public Guid CuentaId { get; set; }
        public string NombreCuenta { get; set; } = null!;
        public string TipoCuenta { get; set; } = null!;
        public decimal SaldoActual { get; set; }
        public string Moneda { get; set; } = null!;
    }
}