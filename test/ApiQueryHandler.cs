using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace faceitApp.Testing
{


public static class ApiQueryHandler
{
    public static async Task QueryFaceitApi(string faceitApiKey, string gameId = "cs2")
    {
        string url = $"https://open.faceit.com/data/v4/players/6b99b73f-2f7c-4e0d-b4a8-c7da09cfb4c2/history?game=cs2&offset=17&limit=1";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + faceitApiKey);

            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Data Retrieved: ");

                // Parse and format the JSON response
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject apiResponse = JObject.Parse(jsonResponse);

                

                string formattedJson = apiResponse.ToString(Newtonsoft.Json.Formatting.Indented); // Pretty-print JSON

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
}