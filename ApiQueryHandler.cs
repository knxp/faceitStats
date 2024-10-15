using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class ApiQueryHandler
{
    public static async Task QueryFaceitApi(string faceitApiKey, string playerId, string gameId = "cs2")
    {
        string url = $"https://open.faceit.com/data/v4/matches/1-f362a5c4-d20c-4a72-85b0-ece0fc27aff9/stats";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + faceitApiKey);

            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {

                // For testing Comment out if change url
                string matchStats = await response.Content.ReadAsStringAsync();
                JObject matchStatsJson = JObject.Parse(matchStats);
                
                JArray matches = (JArray)matchStatsJson["player_stats"];
                string formattedJson = matchStatsJson.ToString(Newtonsoft.Json.Formatting.Indented); // Pretty-print JSON

                Console.WriteLine("Data Retrieved: ");

                // Parse and format the JSON response
                //JObject apiResponse = JObject.Parse(jsonResponse);
                //string formattedJson = apiResponse.ToString(Newtonsoft.Json.Formatting.Indented); // Pretty-print JSON

                // Write formatted JSON to a file
                File.WriteAllText("ApiResponseOutput.txt", formattedJson);

                // Output the formatted JSON to console
                Console.WriteLine(formattedJson);
            }
            else
            {
                Console.WriteLine($"Failed to query API. Status code: {response.StatusCode}");
            }
        }
    }
}
