using Dexter.Configuration;
using Dexter.Core.Abstractions;
using Dexter.Core.Attributes;
using Discord.Commands;

namespace Dexter.Commands {
    [EssentialModule]
    public partial class HelpCommands : ModuleD {

        private readonly CommandService CommandService;
        private readonly BotConfiguration BotConfiguration;

        public HelpCommands(CommandService _CommandService, BotConfiguration _BotConfiguration) {
            CommandService = _CommandService;
            BotConfiguration = _BotConfiguration;
        }

    }
}
