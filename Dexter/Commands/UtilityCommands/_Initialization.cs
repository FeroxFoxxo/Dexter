using Dexter.Abstractions;
using Dexter.Services;
using Discord.WebSocket;

namespace Dexter.Commands {

    public partial class UtilityCommands : DiscordModule {

        public LoggingService LoggingService { get; set; }

        public ProfileService ProfileService { get; set; }

        public DiscordSocketClient DiscordSocketClient { get; set; }

        public StartupService StartupService { get; set; }

    }

}
