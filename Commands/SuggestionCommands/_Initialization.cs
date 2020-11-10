using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Suggestions;
using Dexter.Services;

namespace Dexter.Commands {
    public partial class SuggestionCommands : DiscordModule {

        private readonly SuggestionDB SuggestionDB;
        private readonly SuggestionService SuggestionService;

        public SuggestionCommands(SuggestionDB _SuggestionDB, SuggestionService _SuggestionService) {
            SuggestionDB = _SuggestionDB;
            SuggestionService = _SuggestionService;
        }

    }
}