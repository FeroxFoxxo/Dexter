using Dexter.Abstractions;

namespace Dexter.Configurations {

    /// <summary>
    /// The ModerationConfiguration relates to the logging of reactions etc to a specified logging channel.
    /// </summary>
    
    public class ModerationConfiguration : JSONConfig {

        /// <summary>
        /// The DISABLED REACTION CHANNELS field details which channels the logging of reactions will not occur in.
        /// This is so we do not log extraneous reaction removals from channels like the suggestions or roles channels. 
        /// </summary>
        
        public ulong[] DisabledReactionChannels { get; set; }

        /// <summary>
        /// The WEBHOOK CHANNEL specifies the snowflake ID of the channel in which the moderation channel shall log to.
        /// </summary>
        
        public ulong WebhookChannel { get; set; }

        /// <summary>
        /// The WEBHOOK NAME specifies the name of the webhook that will be instantiated in the given moderation channel. 
        /// </summary>
        
        public string WebhookName { get; set; }

    }

}
