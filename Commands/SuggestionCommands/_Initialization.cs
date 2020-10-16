using Dexter.Core.Abstractions;
using Dexter.Databases.Suggestions;
using Dexter.Services;

namespace Dexter.Commands.SuggestionCommands {
    public partial class SuggestionCommands : ModuleD {

        private readonly SuggestionDB SuggestionDB;
        private readonly SuggestionService SuggestionService;

        public SuggestionCommands(SuggestionDB _SuggestionDB, SuggestionService _SuggestionService) {
            SuggestionDB = _SuggestionDB;
            SuggestionService = _SuggestionService;
        }

    }
}