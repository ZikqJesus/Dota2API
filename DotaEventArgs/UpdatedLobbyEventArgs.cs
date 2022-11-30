using SteamKit2.GC.Dota.Internal;

namespace DotaAPI.DotaEventArgs
{
    public class UpdatedLobbyEventArgs : EventArgs
    {
        public CSODOTALobby? NewLobby { get; set; }
        public CSODOTALobby? OldLobby { get; set; }
    }
}
