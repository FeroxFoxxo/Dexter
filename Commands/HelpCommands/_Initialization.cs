using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Attributes;
using Discord.Commands;
using Discord.WebSocket;
using Dexter.Services;

namespace Dexter.Commands {
    [EssentialModule]
    public partial class HelpCommands : DiscordModule {

        private readonly CommandService CommandService;
        private readonly BotConfiguration BotConfiguration;
        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly ReactionMenuService ReactionMenuService;

        public HelpCommands(CommandService CommandService, BotConfiguration BotConfiguration,
                DiscordSocketClient DiscordSocketClient, ReactionMenuService ReactionMenuService) {
            this.CommandService = CommandService;
            this.BotConfiguration = BotConfiguration;
            this.DiscordSocketClient = DiscordSocketClient;
            this.ReactionMenuService = ReactionMenuService;
        }

    }
}
