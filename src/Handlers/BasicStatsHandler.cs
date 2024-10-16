using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using faceitApp.Dictionaries;

namespace faceitApp.Handlers
{
    public static class BasicStatsHandler
{
    // Function to get average basic stats for CS2 matchmaking games with pagination
    public static async Task GetAverageBasicStats(string faceitApiKey, string playerId)
    {
        int offset = 0;  // Start at the first page of results
        int limit = 20; // Maximum number of matches to request per page
        int totalMatchesPulled = 0; // Keep track of the total number of matches pulled
        bool hasMoreMatches = true; // Flag to determine if more matches exist

        int matchCount = 0;
        int matchLimit = 500; // Max matches to process

        // Reuse the same HttpClient instance for better performance
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + faceitApiKey);

            while (hasMoreMatches && matchCount < matchLimit) // Continue until we get at most 100 matches
            {
                string matchHistoryUrl = $"https://open.faceit.com/data/v4/players/{playerId}/history?game=cs2&offset={offset}&limit={limit}";

                // Get the player's match history for the current page
                HttpResponseMessage response = await client.GetAsync(matchHistoryUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject matchHistory = JObject.Parse(jsonResponse);
                    JArray matches = (JArray)matchHistory["items"];

                    // If no more matches are found, stop the loop
                    if (matches.Count == 0)
                    {
                        hasMoreMatches = false;
                        break;
                    }

                    // Update the total matches pulled so far
                    totalMatchesPulled += matches.Count;
                    Console.WriteLine($"Pulled {totalMatchesPulled} matches so far...");

                    // Create a list of tasks to fetch match stats in parallel
                    List<Task> matchTasks = new List<Task>();

                    foreach (var match in matches)
                    {
                        if (match["competition_type"] != null && match["competition_type"].ToString() == "matchmaking")
                        {
                            string matchId = match["match_id"].ToString();
                            string matchUrl = $"https://open.faceit.com/data/v4/matches/{matchId}/stats";

                            // Add match stats fetching task to the list
                            matchTasks.Add(Task.Run(async () =>
                            {
                                HttpResponseMessage matchResponse = await client.GetAsync(matchUrl);
                                if (matchResponse.IsSuccessStatusCode)
                                {
                                    string matchStats = await matchResponse.Content.ReadAsStringAsync();
                                    JObject matchStatsJson = JObject.Parse(matchStats);

                                    // Ensure "rounds" object exists and has content
                                    if (matchStatsJson["rounds"] != null && matchStatsJson["rounds"].HasValues)
                                    {
                                        foreach (var round in matchStatsJson["rounds"])
                                        {
                                            foreach (var team in round["teams"])
                                            {
                                                foreach (var player in team["players"])
                                                {
                                                    string currentPlayerId = player["player_id"].ToString();

                                                    // Check if the current player matches the playerId
                                                    if (currentPlayerId == playerId)
                                                    {
                                                        // Ensure the "stats" object exists
                                                        if (player["player_stats"] != null)
                                                        {
                                                            JObject stats = (JObject)player["player_stats"];

                                                            // Accumulate stats using the BasicStatDictionary
                                                            foreach (var key in BasicStatDictionary.Stats.Keys)
                                                            {
                                                                if (stats[key] != null)
                                                                {
                                                                    BasicStatDictionary.Stats[key] += stats[key].Value<double>();
                                                                }
                                                            }
                                                            matchCount++;

                                                            // Stop if we've processed 100 matches
                                                            if (matchCount >= matchLimit)
                                                            {
                                                                return;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }));
                        }
                    }

                    // Wait for all match stats to be fetched before proceeding to the next batch
                    await Task.WhenAll(matchTasks);

                    // Increase the offset to get the next page of results
                    offset += limit;
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve match history. Status code: {response.StatusCode}");
                    break;
                }
            }
        }

        // Calculate and display averages
        if (matchCount > 0)
        {
            Console.WriteLine("Average Basic Matchmaking Stats for CS2:");
            foreach (var key in BasicStatDictionary.CustomOrder)
            {
                if (BasicStatDictionary.Stats.TryGetValue(key, out double totalValue))
                {
                    double averageValue = totalValue / matchCount;
                    Console.WriteLine($"{key}: {Math.Round(averageValue, 2)}");
                }
            }
        }
        else
        {
            Console.WriteLine("No matchmaking matches found.");
        }
    }
}
}