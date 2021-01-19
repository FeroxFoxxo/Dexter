using Dexter.Abstractions;

namespace Dexter.Configurations {
    
    /// <summary>
    /// Encompasses all configuration pertaining to the rules about advertising commissions.
    /// </summary>

    public class CommissionCooldownConfiguration : JSONConfig {

        /// <summary>
        /// The numerical ID of the #commissions-corner channel
        /// </summary>

        public ulong CommissionsCornerID { get; set; }

        /// <summary>
        /// The minimum amount of time allowed between two different posts by the same user in #commissions-corner, in seconds.
        /// </summary>

        public long CommissionCornerCooldown { get; set; }

        /// <summary>
        /// The maximum amount of time allowed after posting one message for posting additional ones in the same post, in seconds. 
        /// </summary>

        public long GracePeriod { get; set; }

    }

}
