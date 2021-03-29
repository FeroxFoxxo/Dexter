using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Infractions {

    /// <summary>
    /// Contains all user-specific information used for recordkeeping and automoderation.
    /// </summary>

    public class DexterProfile {

        /// <summary>
        /// The unique numerical identifier for the profile's linked User.
        /// </summary>

        [Key]
        public ulong UserID { get; set; }

        /// <summary>
        /// Total number of recorded infractions
        /// </summary>

        public short InfractionAmount { get; set; }

        /// <summary>
        /// The token associated with the current mute in the corresponding database.
        /// </summary>

        public string CurrentMute { get; set; }

        /// <summary>
        /// The token associated with the current event timer in the corresponding database.
        /// </summary>

        public string CurrentPointTimer { get; set; }
    }

}
