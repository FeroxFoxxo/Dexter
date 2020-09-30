using Dexter.Core.Abstractions;
using Dexter.Databases.Suggestions;
using Discord.Commands;

namespace Dexter.Commands.SuggestionCommands {
    public partial class SuggestionCommands : ModuleBase<CommandModule> {

        private readonly SuggestionDB SuggestionDB;

        public SuggestionCommands(SuggestionDB _SuggestionDB) {
            SuggestionDB = _SuggestionDB;
        }

    }
}
