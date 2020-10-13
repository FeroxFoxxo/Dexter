using Dexter.Abstractions;
using Dexter.Attributes;
using Discord.Commands;

namespace Dexter.Commands.HelpCommands {
    [EssentialModule]
    public partial class HelpCommands : Module {
        private readonly CommandService CommandService;

        public HelpCommands(CommandService _CommandService) {
            CommandService = _CommandService;
        }
    }
}
