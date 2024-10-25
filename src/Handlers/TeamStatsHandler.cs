using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using faceitApp.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace faceitApp.Handlers
{
    public class TeamStatsHandler
    {
        private readonly HttpClient _httpClient;
        private readonly string _faceitApiKey;

        public TeamStatsHandler(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _faceitApiKey = configuration["Faceit:ApiKey"];
        }

        public async Task<(TeamInfo info, TeamStats stats, List<MapStats> mapStats)> GetTeamStatsAsync(string teamId, string gameId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _faceitApiKey);

                // Get team info
                var infoResponse = await _httpClient.GetAsync($"https://open.faceit.com/data/v4/teams/{teamId}");
                infoResponse.EnsureSuccessStatusCode();
                var infoJson = await infoResponse.Content.ReadAsStringAsync();
                var infoData = JObject.Parse(infoJson);

                var teamInfo = new TeamInfo
                {
                    Name = infoData["name"]?.ToString(),
                    Avatar = infoData["avatar"]?.ToString(),
                    GameId = gameId,
                    Members = new List<TeamMember>()
                };

                var members = infoData["members"] as JArray;
                if (members != null)
                {
                    foreach (var member in members)
                    {
                        teamInfo.Members.Add(new TeamMember
                        {
                            Nickname = member["nickname"]?.ToString(),
                            PlayerId = member["user_id"]?.ToString(),
                            Avatar = member["avatar"]?.ToString()
                        });
                    }
                }

                // Get team stats
                var statsResponse = await _httpClient.GetAsync($"https://open.faceit.com/data/v4/teams/{teamId}/stats/{gameId}");
                statsResponse.EnsureSuccessStatusCode();
                var statsJson = await statsResponse.Content.ReadAsStringAsync();
                var statsData = JObject.Parse(statsJson);

                var teamStats = new TeamStats
                {
                    TotalMatches = int.Parse(statsData["lifetime"]?["Matches"]?.ToString() ?? "0"),
                    WinCount = int.Parse(statsData["lifetime"]?["Wins"]?.ToString() ?? "0"),
                    CurrentStreak = int.Parse(statsData["lifetime"]?["Current Win Streak"]?.ToString() ?? "0"),
                    LongestWinStreak = statsData["lifetime"]?["Longest Win Streak"]?.ToString() ?? "0",
                    RecentMatches = new List<TeamMatchHistory>()
                };

                // Get recent results
                var recentResults = statsData["lifetime"]?["Recent Results"] as JArray;
                if (recentResults != null)
                {
                    foreach (var result in recentResults.Take(5))
                    {
                        teamStats.RecentMatches.Add(new TeamMatchHistory
                        {
                            MatchId = Guid.NewGuid().ToString(),
                            TeamId = teamId,
                            Result = result.ToString() == "1" ? 1 : 0
                        });
                    }
                }

                // Get map stats
                var mapStats = new List<MapStats>();
                var segments = statsData["segments"] as JArray;
                if (segments != null)
                {
                    foreach (var segment in segments)
                    {
                        var mapName = segment["label"]?.ToString();
                        if (!string.IsNullOrEmpty(mapName) && !mapName.Contains("wingman", StringComparison.OrdinalIgnoreCase))
                        {
                            mapStats.Add(new MapStats
                            {
                                Map = mapName,
                                TotalMatches = int.Parse(segment["stats"]?["Matches"]?.ToString() ?? "0"),
                                Wins = int.Parse(segment["stats"]?["Wins"]?.ToString() ?? "0")
                            });
                        }
                    }
                }

                return (teamInfo, teamStats, mapStats);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching team stats: {ex.Message}");
            }
        }
    }
}