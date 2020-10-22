using Dexter.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {
    public class FunConfiguration : JSONConfiguration {

        public ulong EmojiGuildID { get; set; }

        public Dictionary<string, ulong> EmojiIDs { get; set; }

        public Dictionary<string, string[]> WouldYouRather { get; set; }

        public Dictionary<string, string[]> Topic { get; set; }

        public Dictionary<string, string[]> EightBall { get; set; }

        public Dictionary<string, string> EightBallEmoji { get; set; }

    }
}
