using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json; // Ensure you have this using directive
using faceitApp.Models; // Ensure you have this namespace for the Player model

namespace faceitApp.Utilities
{
    public class GetPlayerInfo
    {
        private readonly HttpClient _httpClient;
        private readonly string _faceitApiKey;

        public GetPlayerInfo(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _faceitApiKey = configuration["Faceit:ApiKey"];
        }

        public async Task<Player> GetPlayerInfoAsync(string playerId)
        {
            string url = $"https://open.faceit.com/data/v4/players/{playerId}";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _faceitApiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject playerData = JObject.Parse(jsonResponse);
                    var cs2Stats = playerData["games"]["cs2"];

                    return new Player
                    {
                        Nickname = playerData["nickname"]?.ToString(), // Use null-conditional operator
                        Avatar = playerData["avatar"]?.ToString(),
                        Elo = cs2Stats != null ? (int?)cs2Stats["faceit_elo"]?.ToObject<int>() : null, // Handle possible nulls,
                        Level = cs2Stats != null ? (int?)cs2Stats["skill_level"]?.ToObject<int>() : null // Handle possible nulls
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request errors here
                throw new Exception($"Error retrieving player info: {ex.Message}");
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors here
                throw new Exception($"Error parsing player data: {ex.Message}");
            }

            return null;
        }
    }
}
