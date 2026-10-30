using System.Text;
using System.Text.Json;
using JJR.Api.Dtos;
using JJR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JJR.Api.Controllers
{
    [Authorize] // Requiere Token JWT en la cabecera 'Authorization: Bearer <token>'
    [ApiController]
    [Route("api/[controller]")]
    public class PrediccionesController : ControllerBase
    {
        private readonly JjrDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PrediccionesController(JjrDbContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> ObtenerPrediccionesIA()
        {
            // 1. Recopilar métricas de la BD
            var rutas = await _context.Rutas
                .Where(r => r.Activo)
                .Select(r => $"{r.Origen} - {r.Destino}")
                .ToListAsync();

            var totalPasajes = await _context.Pasajes.CountAsync(p => p.Estado != "Anulado");
            var totalEncomiendas = await _context.Encomiendas.CountAsync(e => e.Estado != "ANULADO");
            var egresosTotal = await _context.EgresosOperativos.SumAsync(e => (decimal?)e.Monto) ?? 0m;
            var ingresosTotal = await _context.MovimientosCajas
                .Where(m => m.Categoria == "VENTA_PASAJES" || m.Categoria == "ENCOMIENDAS" || m.Categoria == "INGRESO")
                .SumAsync(m => (decimal?)m.Monto) ?? 0m;

            int totalViajes = await _context.Viajes.CountAsync();

            // 2. Prompt estructurado para JSON
            string prompt = $@"
Eres el motor de Inteligencia Artificial de 'Transportes JJR S.A.C.'.
Analiza las siguientes métricas operativas reales:
- Rutas activas: {(rutas.Count > 0 ? string.Join(", ", rutas) : "Sin rutas configuradas")}
- Total pasajes vendidos: {totalPasajes}
- Total encomiendas atendidas: {totalEncomiendas}
- Total viajes realizados: {totalViajes}
- Ingresos acumulados: S/ {ingresosTotal}
- Egresos operativos acumulados: S/ {egresosTotal}

Devuelve ÚNICAMENTE un objeto JSON válido con la siguiente estructura exacta:
{{
  ""demandaRutas"": [
    {{
      ""ruta"": ""Origen - Destino"",
      ""diaSemana"": ""Próximo Fin de Semana"",
      ""asientosOcupadosPromedio"": 4,
      ""nivelDemandaPorcentaje"": 85.5,
      ""recomendacion"": ""Recomendación ejecutiva para la ruta""
    }}
  ],
  ""proyeccionFinanciera"": [
    {{
      ""mes"": ""Mes Siguiente"",
      ""ingresosProyectados"": 15000.0,
      ""egresosProyectados"": 5000.0,
      ""utilidadEstimada"": 10000.0
    }}
  ],
  ""totalViajesAnalizados"": {totalViajes},
  ""precisionModelo"": 95.8
}}";

            // 3. Obtener API Key
            var apiKey = _configuration["Groq:ApiKey"]
                      ?? _configuration["GROQ_API_KEY"]
                      ?? Environment.GetEnvironmentVariable("Groq__ApiKey")
                      ?? Environment.GetEnvironmentVariable("GROQ_API_KEY");

            var modelName = _configuration["Groq:Model"] ?? "openai/gpt-oss-20b";

            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("AQUI_PEGA"))
            {
                return BadRequest(new { mensaje = "Clave Groq:ApiKey no configurada en appsettings.json o .env" });
            }

            // 4. Armar Request de acuerdo a la especificación de Groq API
            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.2,
                response_format = new { type = "json_object" }
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                if (!response.IsSuccessStatusCode)
                {
                    var errContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { mensaje = "Error devuelto por Groq API", detalle = errContent });
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);

                // Estructura oficial de respuesta Groq: choices[0].message.content
                var contentString = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                // Limpieza preventiva si el modelo incluye Markdown
                if (!string.IsNullOrEmpty(contentString))
                {
                    contentString = contentString.Trim();
                    if (contentString.StartsWith("```json")) contentString = contentString.Substring(7);
                    if (contentString.StartsWith("```")) contentString = contentString.Substring(3);
                    if (contentString.EndsWith("```")) contentString = contentString.Substring(0, contentString.Length - 3);
                    contentString = contentString.Trim();
                }

                var resultado = JsonSerializer.Deserialize<DashboardIAResponseDto>(
                    contentString!,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al procesar las predicciones con IA", error = ex.Message });
            }
        }
    }
}