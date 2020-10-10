using Dexter.Core.Abstractions;
using Dexter.Databases.Suggestions;

namespace Dexter.Commands.SuggestionCommands {
    public partial class SuggestionCommands : Module {

        private readonly SuggestionDB SuggestionDB;

        public SuggestionCommands(SuggestionDB _SuggestionDB) {
            SuggestionDB = _SuggestionDB;
        }

    }
}
