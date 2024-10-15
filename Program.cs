using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: false, reloadOnChange: true)
        .Build();


        // Get the API key
        string faceitApiKey = configuration["Faceit:ApiKey"];

        // Get gameId
        string gameId = configuration["Faceit:gameId"];



        // Get player ID from user input
        //string playerId = await GetPlayerID.GetPlayerIDFromUser(faceitApiKey);

        //Get Team ID from user input
        //string teamId = await GetTeamId.GetTeamIDFromUser();
        string teamId = "2c53c2b2-518f-4885-83bf-85aa32e90286";

        // Query the API for testing
        //await ApiQueryHandler.QueryFaceitApi(faceitApiKey);

        // Full Stats
        //await FullStatsHandler.GetAverageMatchmakingStats(faceitApiKey, playerId);

        // Team Stats
        await TeamStatsHandler.GetTeamStats(faceitApiKey, teamId, gameId);

        // Pass the API key to MatchStatsHandler
        //await PlayerMatchStatsHandler.GetMostRecentMatchStatistics(faceitApiKey, playerId);


    }
}
