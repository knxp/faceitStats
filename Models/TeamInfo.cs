using System.Collections.Generic;

namespace faceitApp.Models
{
    public class TeamInfo
    {
        public string Name { get; set; }
        public string Avatar { get; set; }
        public List<TeamMember> Members { get; set; }
        public string GameId { get; set; }
    }

    public class TeamMember
    {
        public string Nickname { get; set; }
        public string PlayerId { get; set; }
        public string Avatar { get; set; }
    }
}