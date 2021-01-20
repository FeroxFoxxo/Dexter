using Dexter.Abstractions;

namespace Dexter.Configurations {

    /// <summary>
    /// Holds relevant configuration for temporary mutes, generally self-imposed.
    /// </summary>

    public class MuzzleConfiguration : JSONConfig {

        /// <summary>
        /// The duration of the self-imposed mute upon using the ~muzzle command.
        /// </summary>

        public int MuzzleDuration { get; set; }

        /// <summary>
        /// The role ID for the "Muzzled" role.
        /// </summary>

        public ulong MuzzleRoleID { get; set; }

        /// <summary>
        /// The role ID for the "Reaction Muted" role.
        /// </summary>

        public ulong ReactionMutedRoleID { get; set; }

    }

}
