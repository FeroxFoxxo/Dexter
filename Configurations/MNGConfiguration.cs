using Dexter.Abstractions;

namespace Dexter.Configuration {
    public class MNGConfiguration : JSONConfiguration {

        public ulong MeetNGreetChannel { get; set; }

        public string MeetNGreetWebhookURL { get; set; }

    }
}
