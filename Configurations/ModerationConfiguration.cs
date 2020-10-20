using Dexter.Core.Abstractions;

namespace Dexter.Configurations {
    public class ModerationConfiguration : JSONConfiguration {

        public ulong ReactionLogChannel { get; set; }

    }
}
