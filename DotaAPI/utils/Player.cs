using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaAPI.utils
{
    public class ProfileCard
    {
        public uint AccountId { get; set; }
        public uint BandgePoints { get; set; }
        public bool IsPlusSubscriber { get; set; }
        public uint RankTier { get; set; }
        public uint RankTierMmrType { get; set; }
        public uint EventId { get; set; }
        public uint EventPoints { get; set; }
        public ulong FavoriteTeamPacked { get; set; }
        public uint LeaderboardRank { get; set; }
        public uint LeaderboardRankCore { get; set; }
        public uint PlusOriginalStartDate { get; set; }
        public uint PreviousRankTier { get; set; }
        public uint RankTierPeak { get; set; }
        public uint RankTierScore { get; set; }

    }
}
