using Dexter.Abstractions;

namespace Dexter.Configurations {

    /// <summary>
    /// The MNGConfiguration specifies the channel that the meet-n-greet moderation will occur in.
    /// </summary>
    public class MNGConfiguration : JSONConfiguration {

        /// <summary>
        /// The MEET N GREET CHANNEL field is a ulong ID of the channel in which the bot
        /// will check in for deletions or messsage updates.
        /// </summary>
        public ulong MeetNGreetChannel { get; set; }

    }

}
