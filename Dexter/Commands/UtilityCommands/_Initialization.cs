using Dexter.Abstractions;
using Dexter.Services;
using Discord.WebSocket;

namespace Dexter.Commands {
    public partial class UtilityCommands : DiscordModule {

        private readonly LoggingService LoggingService;

        private readonly ProfileService ProfileService;

        private readonly DiscordSocketClient DiscordSocketClient;

        public UtilityCommands(LoggingService LoggingService, ProfileService ProfileService, DiscordSocketClient DiscordSocketClient) {
            this.LoggingService = LoggingService;
            this.ProfileService = ProfileService;
            this.DiscordSocketClient = DiscordSocketClient;
        }

    }
}
