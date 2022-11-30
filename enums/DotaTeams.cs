using SteamKit2.GC.Dota.Internal;

namespace DotaAPI.enums
{
    public enum DotaTeam
    {
        Radiant = DOTA_GC_TEAM.DOTA_GC_TEAM_GOOD_GUYS,
        Dire = DOTA_GC_TEAM.DOTA_GC_TEAM_BAD_GUYS,
        Pool = DOTA_GC_TEAM.DOTA_GC_TEAM_PLAYER_POOL,
        Spectator = DOTA_GC_TEAM.DOTA_GC_TEAM_SPECTATOR,
        NoTeam = DOTA_GC_TEAM.DOTA_GC_TEAM_NOTEAM
    }
}
