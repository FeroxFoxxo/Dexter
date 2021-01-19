using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Relays {
    
    /// <summary>
    /// Sends a preset message in a target channel every time a certain number of messages are sent.
    /// </summary>

    public class Relay {

        /// <summary>
        /// The numerical ID of the target channel the relay is measuring.
        /// </summary>

        [Key]
        public ulong ChannelID { get; set; }

        /// <summary>
        /// The message the relay is to send into the channel every time "CurrentMessageCount" reaches "MessageInterval".
        /// </summary>

        public string Message { get; set; }

        /// <summary>
        /// The amount of messages between each time "Message" is sent.
        /// </summary>

        public int MessageInterval { get; set; }

        /// <summary>
        /// The current count of messages since last time "Message" was sent.
        /// </summary>

        public int CurrentMessageCount { get; set; }

    }

}
