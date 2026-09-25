using System.Net.Http.Headers;
using System.Text.Json;
using JJR.Api.Dtos;

namespace JJR.Api.Services
{
    public class ApiSunatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public ApiSunatService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<ApiSunatResponseDto> EmitirComprobanteAsync(ApiSunatDocumentDto payload)
        {
            var baseUrl = _config["ApiSunat:BaseUrl"];
            var token = _config["ApiSunat:Token"];

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v3/documents");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ApiSunatResponseDto
                {
                    success = false,
                    message = $"Error APISUNAT ({response.StatusCode}): {responseContent}"
                };
            }

            var result = JsonSerializer.Deserialize<ApiSunatResponseDto>(responseContent);
            return result ?? new ApiSunatResponseDto { success = false, message = "Respuesta nula de APISUNAT" };
        }
    }
}