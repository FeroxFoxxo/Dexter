using Dexter.Abstractions;

namespace Dexter.Configurations {
    public class ModerationConfiguration : JSONConfiguration {

        public string ModerationWebhookURL { get; set; }

        public ulong[] DisabledReactionChannels { get; set; }

    }
}
