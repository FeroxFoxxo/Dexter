using Dexter.Core.DiscordApp;
using Dexter.Databases.Suggestions;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands.SuggestionCommands {
    public partial class SuggestionCommands {

        [Command("fetchSuggestion")]
        [Summary("Fetches a suggestion")]
        [Alias("suggestionFetch")]
        [RequireModerator]
        public async Task FetchSuggestion(string SuggestionProperty) {
            Suggestion Suggestion = SuggestionDB.Suggestions.AsQueryable().Where(Suggestions => Suggestions.Tracker == SuggestionProperty ||
                Suggestions.MessageID == SuggestionProperty || Suggestions.StaffMessageID == SuggestionProperty).FirstOrDefault();

            if(Suggestion != null) {
                
            } else {

            }
        }
    }
}
