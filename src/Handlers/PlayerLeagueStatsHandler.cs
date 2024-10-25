using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using faceitApp.Models;

namespace faceitApp.Handlers
{
    public class PlayerLeagueStatsHandler
    {
        private readonly HttpClient _httpClient;
        private readonly string _faceitApiKey;
        private readonly Regex _eseaRegex = new Regex(
            @"ESEA\s+S(\d+)\s+(\w+)\s+(\w+)\s+(\w+)\s*-\s*(.+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public PlayerLeagueStatsHandler(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _faceitApiKey = configuration["Faceit:ApiKey"];
        }

        public async Task<LeagueStatsCollection> GetLeagueStatsAsync(string playerId)
        {
            var collection = new LeagueStatsCollection();
            var seasonStats = new Dictionary<string, (LeagueStats Stats, int MatchCount)>();
            var overallStats = new Player { Id = playerId };

            int offset = 0;
            const int limit = 100;
            int totalMatches = 0;
            bool hasMoreMatches = true;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _faceitApiKey);

            while (hasMoreMatches)
            {
                var matchHistoryUrl = $"https://open.faceit.com/data/v4/players/{playerId}/history?game=cs2&offset={offset}&limit={limit}";
                var response = await _httpClient.GetAsync(matchHistoryUrl);

                if (!response.IsSuccessStatusCode)
                    break;

                var matchHistory = JObject.Parse(await response.Content.ReadAsStringAsync());
                var matches = matchHistory["items"] as JArray;

                if (matches == null || !matches.Any())
                    break;

                var eseaMatches = matches
                    .Where(m =>
                        m["competition_type"]?.ToString() == "championship" &&
                        m["competition_name"]?.ToString().Contains("ESEA", StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                if (!eseaMatches.Any())
                {
                    offset += limit;
                    continue;
                }

                var matchTasks = eseaMatches.Select(match => ProcessMatchAsync(match, playerId));
                var results = await Task.WhenAll(matchTasks);

                foreach (var result in results.Where(r => r.HasValue))
                {
                    var (matchStats, leagueInfo) = result.Value;
                    totalMatches++;

                    var seasonKey = $"{leagueInfo.Season}-{leagueInfo.Division}-{leagueInfo.Location}";

                    // Update season stats
                    if (!seasonStats.ContainsKey(seasonKey))
                    {
                        seasonStats[seasonKey] = (new LeagueStats
                        {
                            Season = leagueInfo.Season,
                            Division = leagueInfo.Division,
                            Location = leagueInfo.Location,
                            DivisionLocation = leagueInfo.DivisionLocation,
                            GameType = leagueInfo.GameType,
                            Stats = new Player { Id = playerId }
                        }, 0);
                    }

                    UpdateStats(seasonStats[seasonKey].Stats.Stats, matchStats);
                    seasonStats[seasonKey] = (seasonStats[seasonKey].Stats, seasonStats[seasonKey].MatchCount + 1);

                    // Update overall stats
                    UpdateStats(overallStats, matchStats);
                }

                offset += limit;
                hasMoreMatches = matches.Count == limit;
            }

            // Calculate averages and build collection
            foreach (var (_, (stats, matchCount)) in seasonStats)
            {
                CalculateAverages(stats.Stats, matchCount);
                stats.MatchCount = matchCount;
                collection.SeasonStats.Add(stats);
            }

            CalculateAverages(overallStats, totalMatches);
            collection.OverallStats = overallStats;
            collection.TotalMatches = totalMatches;

            return collection;
        }

        private async Task<(Player Stats, LeagueStats LeagueInfo)?> ProcessMatchAsync(JToken match, string playerId)
        {
            try
            {
                var matchId = match["match_id"].ToString();
                var competitionName = match["competition_name"]?.ToString() ?? "";

                var eseaMatch = _eseaRegex.Match(competitionName);
                if (!eseaMatch.Success)
                    return null;

                var leagueInfo = new LeagueStats
                {
                    Season = $"S{eseaMatch.Groups[1].Value}",
                    Location = eseaMatch.Groups[2].Value,
                    Division = eseaMatch.Groups[3].Value,
                    DivisionLocation = eseaMatch.Groups[4].Value,
                    GameType = eseaMatch.Groups[5].Value.Trim()
                };

                var matchStatsUrl = $"https://open.faceit.com/data/v4/matches/{matchId}/stats";
                var response = await _httpClient.GetAsync(matchStatsUrl);

                if (!response.IsSuccessStatusCode)
                    return null;

                var matchStats = JObject.Parse(await response.Content.ReadAsStringAsync());
                var rounds = matchStats["rounds"] as JArray;

                if (rounds == null || !rounds.Any())
                    return null;

                var playerStats = rounds[0]["teams"]
                    .SelectMany(t => t["players"])
                    .FirstOrDefault(p => p["player_id"]?.ToString() == playerId)?["player_stats"] as JObject;

                if (playerStats == null)
                    return null;

                return (ParsePlayerStats(playerStats), leagueInfo);
            }
            catch
            {
                return null;
            }
        }

        private Player ParsePlayerStats(JObject stats)
        {
            return new Player
            {
                Kills = ParseStat(stats["Kills"]),
                Deaths = ParseStat(stats["Deaths"]),
                Assists = ParseStat(stats["Assists"]),
                KDRatio = ParseStat(stats["K/D Ratio"]),
                KRRRatio = ParseStat(stats["K/R Ratio"]),
                HeadshotsPercentage = ParseStat(stats["Headshots %"]),
                Headshots = ParseStat(stats["Headshots"]),
                MVPs = ParseStat(stats["MVPs"]),
                TripleKills = ParseStat(stats["Triple Kills"]),
                QuadroKills = ParseStat(stats["Quadro Kills"]),
                PentaKills = ParseStat(stats["Penta Kills"])
            };
        }

        private double ParseStat(JToken stat)
        {
            if (stat == null)
                return 0;

            var value = stat.ToString();
            if (value.EndsWith("%"))
                value = value.TrimEnd('%');

            return double.TryParse(value, out var result) ? result : 0;
        }

        private void UpdateStats(Player target, Player source)
        {
            target.Kills += source.Kills;
            target.Deaths += source.Deaths;
            target.Assists += source.Assists;
            target.KDRatio += source.KDRatio;
            target.KRRRatio += source.KRRRatio;
            target.HeadshotsPercentage += source.HeadshotsPercentage;
            target.Headshots += source.Headshots;
            target.MVPs += source.MVPs;
            target.TripleKills += source.TripleKills;
            target.QuadroKills += source.QuadroKills;
            target.PentaKills += source.PentaKills;
        }

        private void CalculateAverages(Player stats, int matchCount)
        {
            if (matchCount == 0)
                return;

            stats.Kills /= matchCount;
            stats.Deaths /= matchCount;
            stats.Assists /= matchCount;
            stats.KDRatio /= matchCount;
            stats.KRRRatio /= matchCount;
            stats.HeadshotsPercentage /= matchCount;
            stats.Headshots /= matchCount;
            stats.MVPs /= matchCount;
            stats.TripleKills /= matchCount;
            stats.QuadroKills /= matchCount;
            stats.PentaKills /= matchCount;
        }
    }
}