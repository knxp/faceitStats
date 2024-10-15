using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class GetPlayerID
{
    // Function to fetch the player ID based on the provided nickname
    public static async Task<string> FetchPlayerID(string nickname, string faceitApiKey)
    {
        string url = $"https://open.faceit.com/data/v4/players?nickname={nickname}";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + faceitApiKey);

            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject playerData = JObject.Parse(jsonResponse);

                // Check if player data is returned
                if (playerData["player_id"] != null)
                {
                    string playerId = playerData["player_id"].ToString();
                    return playerId;
                }
                else
                {
                    Console.WriteLine("Player not found.");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"Error fetching player ID: {response.StatusCode}");
                return null;
            }
        }
    }

    // Method to prompt user for nickname and return player ID
    public static async Task<string> GetPlayerIDFromUser(string faceitApiKey)
    {
        Console.Write("Enter the Faceit player's nickname: ");
        string nickname = Console.ReadLine();
        return await FetchPlayerID(nickname, faceitApiKey);
    }
}
