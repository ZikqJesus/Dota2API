using DotaAPI.enums;
using SteamKit2.GC.Dota.Internal;

namespace DotaAPI.DotaEventArgs
{
    internal class MatchEndEventArgs : EventArgs
    {
        public DotaTeam Winner { get; set; }
        public List<string> RadiantHeroes { get; set; }
        public List<string> DireHeroes { get; set; }
        public List<string> RadiantPlayers { get; set; }
        public List<string> DirePlayers { get; set; }
        public CSODOTALobby Lobby { get; set; }

    }
}
