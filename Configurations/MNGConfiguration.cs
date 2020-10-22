using Dexter.Abstractions;

namespace Dexter.Configurations {
    public class MNGConfiguration : JSONConfiguration {

        public ulong MeetNGreetChannel { get; set; }

        public string MeetNGreetWebhookURL { get; set; }

    }
}
