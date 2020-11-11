using Dexter.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {

    /// <summary>
    /// The FunConfiguration relates to attributes required by the FunCommands module.
    /// </summary>
    public class FunConfiguration : JSONConfiguration {

        /// <summary>
        /// The EMOJI GUILD ID field is the snowflake ID of the server in which the eight-ball and gay emojis are stored.
        /// </summary>
        public ulong EmojiGuildID { get; set; }

        /// <summary>
        /// The EMOJI ID field is a dictionary of the type of emoji (EG love, annoyed, wut) and their corresponding emoji IDs.
        /// </summary>
        public Dictionary<string, ulong> EmojiIDs { get; set; }

        /// <summary>
        /// The EIGHT BALL field specifies the responces the eight-ball command can give.
        /// </summary>
        public Dictionary<string, string[]> EightBall { get; set; }

        /// <summary>
        /// The EIGHT BALL EMOJI field links the type of responce the eight-ball command gives to its corresponding emoji in the EMOJI IDs.
        /// </summary>
        public Dictionary<string, string> EightBallEmoji { get; set; }

    }

}
