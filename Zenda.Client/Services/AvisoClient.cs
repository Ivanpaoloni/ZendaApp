using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Zenda.Core.DTOs;

namespace Zenda.Client.Services
{
    public class AvisoClient : BaseClient
    {
        private readonly HttpClient _httpClient;

        public AvisoClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AvisoDto?> GetAvisoActivoAsync()
        {
            var response = await _httpClient.GetAsync("api/avisos/activo");

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AvisoDto>();
        }

        public async Task<List<AvisoDto>> GetAllAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<AvisoDto>>("api/avisos") ?? new List<AvisoDto>();
        }

        public async Task<AvisoDto> CreateAsync(AvisoDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/avisos", dto);

            if (!response.IsSuccessStatusCode)
            {
                // Usamos tu método ParseError heredado para manejar validaciones y excepciones
                var errorRaw = await response.Content.ReadAsStringAsync();
                throw new Exception(ParseError(errorRaw));
            }

            return await response.Content.ReadFromJsonAsync<AvisoDto>()
                   ?? throw new Exception("Error al leer la respuesta del servidor.");
        }

        public async Task<AvisoDto> UpdateAsync(Guid id, AvisoDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/avisos/{id}", dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorRaw = await response.Content.ReadAsStringAsync();
                throw new Exception(ParseError(errorRaw));
            }

            return await response.Content.ReadFromJsonAsync<AvisoDto>()
                   ?? throw new Exception("Error al leer la respuesta del servidor.");
        }

        public async Task ActivarAsync(Guid id)
        {
            var response = await _httpClient.PutAsync($"api/avisos/{id}/activar", null);

            if (!response.IsSuccessStatusCode)
            {
                var errorRaw = await response.Content.ReadAsStringAsync();
                throw new Exception(ParseError(errorRaw));
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/avisos/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorRaw = await response.Content.ReadAsStringAsync();
                throw new Exception(ParseError(errorRaw));
            }
        }

        // Dentro de AvisoClient:
        public async Task<string> UploadImageAsync(MultipartFormDataContent content)
        {
            var response = await _httpClient.PostAsync("api/avisos/upload-image", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorRaw = await response.Content.ReadAsStringAsync();
                throw new Exception(ParseError(errorRaw));
            }

            var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
            return result?.Url ?? throw new Exception("Error al obtener la URL de la imagen.");
        }
    }
}