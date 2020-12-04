using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Attributes;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Commands {

    [EssentialModule]
    public partial class HelpCommands : DiscordModule {

        private readonly CommandService CommandService;
        private readonly BotConfiguration BotConfiguration;
        private readonly DiscordSocketClient DiscordSocketClient;

        public HelpCommands(CommandService CommandService, BotConfiguration BotConfiguration,
                DiscordSocketClient DiscordSocketClient) {
            this.CommandService = CommandService;
            this.BotConfiguration = BotConfiguration;
            this.DiscordSocketClient = DiscordSocketClient;
        }

    }

}
