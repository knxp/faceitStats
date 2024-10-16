using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace faceitApp.Utilities
{
    public static class GetTeamId
{
    public static string ExtractTeamId(string teamLink)
    {
        // Check if the link is valid and contains the team ID
        if (Uri.TryCreate(teamLink, UriKind.Absolute, out Uri uriResult))
        {
            // Split the path segments and check for the team ID
            string[] segments = uriResult.Segments;

            if (segments.Length > 3 && segments[2].Equals("teams/", StringComparison.OrdinalIgnoreCase))
            {
                // The team ID is the next segment
                return segments[3].TrimEnd('/');
            }
        }

        throw new ArgumentException("Invalid team link format. Please provide a valid Faceit team link.");
    }

    public static async Task<string> GetTeamIDFromUser()
    {
        Console.Write("Enter the Faceit team link: ");
        string teamLink = Console.ReadLine();
        return ExtractTeamId(teamLink);
    }
}
}