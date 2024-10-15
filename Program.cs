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
        string faceitApiKey = configuration["FaceitApi:ApiKey"];

       
        // Get player ID from user input
        string playerId = await GetPlayerID.GetPlayerIDFromUser(faceitApiKey);

        // Query the API for testing
        //await ApiQueryHandler.QueryFaceitApi(faceitApiKey, playerId);

        // Full Stats
        //await FullStatsHandler.GetAverageMatchmakingStats(faceitApiKey, playerId);


        // Pass the API key to MatchStatsHandler
        //await PlayerMatchStatsHandler.GetMostRecentMatchStatistics(faceitApiKey, playerId);


    }
}
