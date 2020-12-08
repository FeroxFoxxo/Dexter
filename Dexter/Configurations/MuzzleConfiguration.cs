using Dexter.Abstractions;

namespace Dexter.Configurations {

    public class MuzzleConfiguration : JSONConfig {

        public int MuzzleDuration { get; set; }

        public ulong MuzzleRoleID { get; set; }

        public ulong ReactionMutedRoleID { get; set; }

    }

}
