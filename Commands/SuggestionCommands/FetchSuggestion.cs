using Dexter.Core.DiscordApp;
using Dexter.Databases;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands.SuggestionCommands {
    public partial class SuggestionCommands {

        [Command("fetchSuggestion")]
        [Summary("Fetches a suggestion based on a message's ID or the suggestion's tracker.")]
        [Alias("suggestionFetch")]
        [RequireModerator]
        public async Task FetchSuggestion(string SuggestionProperty) {
            Suggestion Suggestion = await SuggestionDB.Suggestions.AsAsyncEnumerable().Where(Suggestions => Suggestions.TrackerID == SuggestionProperty ||
                Suggestions.MessageID.ToString() == SuggestionProperty || Suggestions.StaffMessageID.ToString() == SuggestionProperty).FirstOrDefaultAsync();

            if(Suggestion != null) {
                
            } else
                await Context.Channel.SendMessageAsync($"Haiya! It doesn't seem as though any suggestions matching '{SuggestionProperty}' exists in the database! Are you sure it's a tracker or message id?");
        }

    }
}
