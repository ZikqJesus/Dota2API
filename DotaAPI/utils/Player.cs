using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;

namespace DotaAPI.utils
{
    public class ProfileCard
    {
        public ProfileCard(IPacketGCMsg packetMsg)
        {
            var msg = new ClientGCMsgProtobuf<CMsgDOTAProfileCard>(packetMsg);
            try
            {
                AccountId = msg.Body.account_id;
                BandgePoints = msg.Body.badge_points;
                IsPlusSubscriber = msg.Body.is_plus_subscriber;
                RankTier = msg.Body.rank_tier;
                RankTierMmrType = msg.Body.rank_tier_mmr_type;
                EventId = msg.Body.event_id;
                EventPoints = msg.Body.event_points;
                FavoriteTeamPacked = msg.Body.favorite_team_packed;
                LeaderboardRank = msg.Body.leaderboard_rank;
                LeaderboardRankCore = msg.Body.leaderboard_rank_core;
                PlusOriginalStartDate = msg.Body.plus_original_start_date;
                PreviousRankTier = msg.Body.previous_rank_tier;
                RankTierPeak = msg.Body.rank_tier_peak;
                RankTierScore = msg.Body.rank_tier_score;
            }
            catch
            {
                AccountId = 0;
            }
            
        }
        public uint AccountId { get; private set; }
        public uint BandgePoints { get; private set; }
        public bool IsPlusSubscriber { get; private set; }
        public uint RankTier { get; private set; }
        public uint RankTierMmrType { get; private set; }
        public uint EventId { get; private set; }
        public uint EventPoints { get; private set; }
        public ulong FavoriteTeamPacked { get; private set; }
        public uint LeaderboardRank { get; private set; }
        public uint LeaderboardRankCore { get; private set; }
        public uint PlusOriginalStartDate { get; private set; }
        public uint PreviousRankTier { get; private set; }
        public uint RankTierPeak { get; private set; }
        public uint RankTierScore { get; private set; }

    }
}
