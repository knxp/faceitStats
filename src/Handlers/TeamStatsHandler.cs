using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using faceitApp.Dictionaries;

namespace faceitApp.Handlers
{
    public static class TeamStatsHandler
{
    public static async Task GetTeamStats(string faceitApiKey, string teamId, string gameId)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + faceitApiKey);
            string teamStatsUrl = $"https://open.faceit.com/data/v4/teams/{teamId}/stats/{gameId}";

            // Fetching team name
            string teamName = await GetTeamName(client, teamId);
            Console.WriteLine($"Team Name: {teamName}");

            // Fetching team stats
            HttpResponseMessage response = await client.GetAsync(teamStatsUrl);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject stats = JObject.Parse(jsonResponse);

                // Fetching lifetime stats
                var lifetime = stats["lifetime"];
                TeamStatsDictionary.TeamStats lifetimeStats = new TeamStatsDictionary.TeamStats
                {
                    Matches = lifetime["Matches"]?.Value<int>() ?? 0,
                    Wins = lifetime["Wins"]?.Value<int>() ?? 0,
                    WinRate = lifetime["Win Rate %"]?.Value<double>() ?? 0,
                    WinStreak = lifetime["Current Win Streak"]?.Value<int>() ?? 0
                };

                // Outputting lifetime stats
                Console.WriteLine($"Lifetime Stats - Matches: {lifetimeStats.Matches}, Wins: {lifetimeStats.Wins}, Win Rate: {lifetimeStats.WinRate}%, Win Streak: {lifetimeStats.WinStreak}");

                // Fetching segment stats
                var segments = stats["segments"] as JArray;
                if (segments != null)
                {
                    foreach (var segment in segments)
                    {
                        string mapName = segment["label"]?.ToString() ?? "Unknown Map";

                        // Skip the segment for Overpass
                        if (mapName.Equals("Overpass", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip this iteration
                        }

                        var segmentStats = segment["stats"];
                        var mapStats = new TeamStatsDictionary.MapStats
                        {
                            Matches = segmentStats["Matches"]?.Value<int>() ?? 0,
                            Wins = segmentStats["Wins"]?.Value<int>() ?? 0,
                            WinRate = segmentStats["Win Rate %"]?.Value<double>() ?? 0
                        };

                        Console.WriteLine($"Map: {mapName}, Matches: {mapStats.Matches}, Wins: {mapStats.Wins}, Win Rate: {mapStats.WinRate}%");
                    }
                }
                else
                {
                    Console.WriteLine("No segment stats available.");
                }
            }
            else
            {
                Console.WriteLine($"Failed to retrieve team stats. Status code: {response.StatusCode}");
            }
        }
    }

    private static async Task<string> GetTeamName(HttpClient client, string teamId)
    {
        string teamInfoUrl = $"https://open.faceit.com/data/v4/teams/{teamId}";
        HttpResponseMessage response = await client.GetAsync(teamInfoUrl);

        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject teamData = JObject.Parse(jsonResponse);
            return teamData["name"]?.ToString() ?? "Unknown Team";
        }

        return "Unknown Team";
    }
}
}