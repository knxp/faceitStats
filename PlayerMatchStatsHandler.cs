using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class PlayerMatchStatsHandler
{
    // Function to get the most recent CS2 match and its statistics
    public static async Task GetMostRecentMatchStatistics(string faceitApiKey, string playerId)
    {
        string matchHistoryUrl = $"https://open.faceit.com/data/v4/players/{playerId}/history?game=cs2"; 

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + faceitApiKey);

            // Get the player's most recent match history
            HttpResponseMessage response = await client.GetAsync(matchHistoryUrl);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject matchHistory = JObject.Parse(jsonResponse);
                JArray matches = (JArray)matchHistory["items"];

                // Filter to find the most recent matchmaking game
                foreach (var match in matches)
                {
                    if (match["competition_type"] != null && match["competition_type"].ToString() == "matchmaking") 
                    {
                        string matchId = match["match_id"].ToString();
                        string matchUrl = $"https://open.faceit.com/data/v4/matches/{matchId}/stats";

                        Console.WriteLine($"Fetching stats for the most recent matchmaking match ID: {matchId}");

                        HttpResponseMessage matchResponse = await client.GetAsync(matchUrl);
                        if (matchResponse.IsSuccessStatusCode)
                        {
                            string matchStats = await matchResponse.Content.ReadAsStringAsync();
                            JObject matchStatsJson = JObject.Parse(matchStats);

                            // Ensure "rounds" object exists and has content
                            if (matchStatsJson["rounds"] != null && matchStatsJson["rounds"].HasValues)
                            {
                                // Loop through the rounds to find the stats for pxnk
                                foreach (var round in matchStatsJson["rounds"])
                                {
                                    // Check both teams for pxnk's stats
                                    foreach (var team in round["teams"])
                                    {
                                        foreach (var player in team["players"])
                                        {
                                            string currentPlayerId = player["player_id"].ToString();
                                            string nickname = player["nickname"].ToString();

                                            // Check if the current player matches playerid
                                            if (currentPlayerId == playerId)
                                            {
                                                Console.WriteLine($"Stats for {nickname}:");

                                                // Ensure the "stats" object exists
                                                if (player["player_stats"] != null)
                                                {
                                                    JObject stats = (JObject)player["player_stats"];

                                                    // Print only pxnk's stats
                                                    Console.WriteLine($"Kills: {stats["Kills"] ?? "N/A"}");
                                                    Console.WriteLine($"Deaths: {stats["Deaths"] ?? "N/A"}");
                                                    Console.WriteLine($"K/D Ratio: {stats["K/D Ratio"] ?? "N/A"}");
                                                    Console.WriteLine($"Headshots: {stats["Headshots"] ?? "N/A"}");
                                                    Console.WriteLine($"ADR: {stats["ADR"] ?? "N/A"}");
                                                    // Add any other stats you want here
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"No stats available for {nickname}.");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("No 'rounds' data found for this match.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Failed to fetch stats for match ID: {matchId}. Status code: {matchResponse.StatusCode}");
                        }

                        // Exit after finding the first matchmaking match
                        return;
                    }
                }

                Console.WriteLine("No recent matchmaking matches found.");
            }
            else
            {
                Console.WriteLine($"Failed to retrieve match history. Status code: {response.StatusCode}");
            }
        }
    }
}
