using Dexter.Core.Abstractions;
using System.Collections.Generic;

namespace Dexter.Core.Configuration {
    public class FunConfiguration : JSONConfiguration {
        public Dictionary<string, string> EmojiIDs { get; set; }

        public Dictionary<string, string[]> WouldYouRather { get; set; }

        public Dictionary<string, string[]> Topic { get; set; }

        public Dictionary<string, string[]> EightBall { get; set; }

        public Dictionary<string, string> EightBallEmoji { get; set; }
    }
}
