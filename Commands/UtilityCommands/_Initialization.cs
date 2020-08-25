using Dexter.Core.Abstractions;
using Dexter.Core.DiscordApp;
using Discord.Commands;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands : ModuleBase<CommandModule> {

        private readonly CommandHandler CommandHandler;

        public UtilityCommands(CommandHandler _CommandHandler) {
            CommandHandler = _CommandHandler;
        }

    }
}
