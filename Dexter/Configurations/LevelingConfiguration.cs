using Dexter.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {

    public class LevelingConfiguration : JSONConfig {

        public int XPIncrementTime { get; set; }

        public int VCMinXPGiven { get; set; }

        public int VCMaxXPGiven { get; set; }

        public ulong GuildID { get; set; }

        public Dictionary<int, ulong> Levels { get; set; }

        public ulong VoiceTextChannel { get; set; }

    }

}
