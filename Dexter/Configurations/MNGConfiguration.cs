using Dexter.Abstractions;

namespace Dexter.Configurations {

    /// <summary>
    /// The MNGConfiguration specifies the channel that the meet-n-greet moderation will occur in.
    /// </summary>
    
    public class MNGConfiguration : JSONConfig {

        /// <summary>
        /// The MEET N GREET CHANNEL field is a ulong ID of the channel in which the bot
        /// will check in for deletions or messsage updates.
        /// </summary>
        
        public ulong MeetNGreetChannel { get; set; }

        /// <summary>
        /// The WEBHOOK CHANNEL specifies the snowflake ID of the channel in which the greetfur logs channel shall log to.
        /// </summary>
        
        public ulong WebhookChannel { get; set; }

        /// <summary>
        /// The WEBHOOK NAME specifies the name of the webhook that will be instantiated in the given greetfur log channel. 
        /// </summary>
        
        public string WebhookName { get; set; }

    }

}
