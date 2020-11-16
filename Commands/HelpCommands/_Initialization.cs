using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Attributes;
using Discord.Commands;

namespace Dexter.Commands {
    [EssentialModule]
    public partial class HelpCommands : DiscordModule {

        private readonly CommandService CommandService;
        private readonly BotConfiguration BotConfiguration;

        public HelpCommands(CommandService CommandService, BotConfiguration BotConfiguration) {
            this.CommandService = CommandService;
            this.BotConfiguration = BotConfiguration;
        }

    }
}
