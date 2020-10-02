using Dexter.Core.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {
    public class SuggestionConfiguration : JSONConfiguration {
        
        public ulong SuggestionGuild { get; set; }

        public ulong SuggestionsChannel { get; set; }

        public ulong StaffSuggestionsChannel { get; set; }

        public ulong EmojiStorageGuild { get; set; }

        public Dictionary<string, string> SuggestionColors { get; set; }

        public Dictionary<string, ulong> Emoji { get; set; }

        public int ReactionPass { get; set; }

    }
}
