using Dexter.Core.Abstractions;
using Dexter.Core.Attributes;
using Discord.Commands;

namespace Dexter.Commands.HelpCommands {
    [EssentialModule]
    public partial class HelpCommands : ModuleD {

        private readonly CommandService CommandService;

        public HelpCommands(CommandService _CommandService) {
            CommandService = _CommandService;
        }

    }
}
