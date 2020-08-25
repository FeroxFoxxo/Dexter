using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord.Commands;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands : ModuleBase<CommandModule> {

        private readonly FunConfiguration FunConfiguration;

        public FunCommands(FunConfiguration _FunConfiguration) {
            FunConfiguration = _FunConfiguration;
        }

    }
}
