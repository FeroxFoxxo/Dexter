using Dexter.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {
    
    /// <summary>
    /// Encompasses all configuration pertaining to the rules about advertising commissions and etc.
    /// </summary>

    public class ChannelCooldownConfiguration : JSONConfig {

        /// <summary>
        /// A list of channels on a cooldown.
        /// 
        /// - KEY ( COOLDOWN CHANNEL ID )
        /// The numerical ID of the #commissions-corner channel
        /// 
        /// - COOLDOWN TIME
        /// The minimum amount of time allowed between two different posts by the same user in #commissions-corner, in seconds.
        /// 
        /// - GRACE PERIOD
        /// The maximum amount of time allowed after posting one message for posting additional ones in the same post, in seconds. 
        /// 
        /// </summary>

        public Dictionary<ulong, Dictionary<string, long>> ChannelCooldowns { get; set; }

    }

}
