using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HumanitarianAidPlatform.Infrastructure.External
{
    public class MappingClient
    {
        private readonly HttpClient _httpClient;

        public MappingClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "HumanitarianAidPlatform");
            }
        }

        public async Task<string?> GetCoordinatesAsync(string address)
        {
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url);
                if (response != null && response.Count > 0)
                {
                    return $"Lat: {response[0].Lat}, Lon: {response[0].Lon}";
                }
            }
            catch (Exception)
            {
                // Handle or log exception accordingly
            }

            return null;
        }
    }

    
    internal class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }
}