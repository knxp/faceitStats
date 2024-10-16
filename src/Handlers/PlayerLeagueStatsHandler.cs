using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using faceitApp.Dictionaries;

public static class PlayerLeagueStatsHandler
{
    // Function to get stats for championship (league) matches, recording both full and basic stats
    public static async Task GetLeagueStats(string faceitApiKey, string playerId)
    {
        int offset = 0;  // Start at the first page of results
        int limit = 100; // Maximum number of matches to request per page
        bool hasMoreMatches = true; // Flag to determine if more matches exist
        int totalMatchesPulled = 0;
        long fromDate = 1718572800; // Unix timestamp for June 17, 2024
        long toDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // Current timestamp

        int championshipMatchCount = 0;
        int matchLimit = 100; // Max matches to process

        // Reuse the same HttpClient instance for better performance
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + faceitApiKey);

            while (hasMoreMatches && championshipMatchCount < matchLimit) // Continue until we hit match limit
            {
                string matchHistoryUrl = $"https://open.faceit.com/data/v4/players/{playerId}/history?game=cs2&offset={offset}&limit={limit}&from={fromDate}&to={toDate}";

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

                    // Create a list of tasks to fetch match stats in parallel
                    List<Task> matchTasks = new List<Task>();

                    foreach (var match in matches)
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

                                                        // If it's a championship match, record both full and basic stats
                                                        if (match["competition_type"] != null && match["competition_type"].ToString() == "championship")
                                                        {
                                                            // Accumulate full stats using FullStatsDictionary
                                                            foreach (var key in FullStatsDictionary.Stats.Keys)
                                                            {
                                                                if (stats[key] != null)
                                                                {
                                                                    FullStatsDictionary.Stats[key] += stats[key].Value<double>();
                                                                }
                                                            }

                                                            // Accumulate basic stats using BasicStatDictionary
                                                            foreach (var key in BasicStatDictionary.Stats.Keys)
                                                            {
                                                                if (stats[key] != null)
                                                                {
                                                                    BasicStatDictionary.Stats[key] += stats[key].Value<double>();
                                                                }
                                                            }

                                                            championshipMatchCount++;

                                                            // Stop if we've processed 100 matches
                                                            if (championshipMatchCount >= matchLimit)
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
                            }
                        }));
                    }

                    // Wait for all match stats to be fetched before proceeding to the next batch
                    await Task.WhenAll(matchTasks);

                    // Update total matches pulled
                    totalMatchesPulled += matches.Count;
                    Console.WriteLine($"Pulled {totalMatchesPulled} matches so far...");

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

        // Display Full Stats for championship matches
        if (championshipMatchCount > 0)
        {
            Console.WriteLine("Average Championship (League) Stats for CS2:");
            foreach (var key in FullStatsDictionary.CustomOrder)
            {
                if (FullStatsDictionary.Stats.TryGetValue(key, out double totalValue))
                {
                    double averageValue = totalValue / championshipMatchCount;
                    Console.WriteLine($"{key}: {Math.Round(averageValue, 2)}");
                }
            }
        }
        else
        {
            Console.WriteLine("No championship matches found.");
        }

        // Display Basic Stats for championship matches
        if (championshipMatchCount > 0)
        {
            Console.WriteLine("\nAverage Basic Stats for Championship CS2 Matches:");
            foreach (var key in BasicStatDictionary.Stats.Keys)
            {
                if (BasicStatDictionary.Stats.TryGetValue(key, out double totalValue))
                {
                    double averageValue = totalValue / championshipMatchCount;
                    Console.WriteLine($"{key}: {Math.Round(averageValue, 2)}");
                }
            }
        }
        else
        {
            Console.WriteLine("No championship matches found.");
        }
    }
}
