using Dexter.Abstractions;

namespace Dexter.Configurations {

    /// <summary>
    /// The ModerationConfiguration relates to the logging of reactions etc to a specified logging channel.
    /// </summary>
    public class ModerationConfiguration : JSONConfiguration {

        /// <summary>
        /// The DISABLED REACTION CHANNELS field details which channels the logging of reactions will not occur in.
        /// This is so we do not log extraneous reaction removals from channels like the suggestions or roles channels. 
        /// </summary>
        public ulong[] DisabledReactionChannels { get; set; }

    }

}
