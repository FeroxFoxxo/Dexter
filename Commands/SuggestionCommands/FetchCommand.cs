using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Suggestions;
using Dexter.Services;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class SuggestionCommands {

        [Command("fetch")]
        [Summary("Fetches a suggestion from the tracker or a message ID.")]
        [Alias("find")]

        public async Task FetchAsync(string Tracker) {
            Suggestion Suggestion = SuggestionDB.GetSuggestionByNameOrID(Tracker);

            if (Suggestion == null)
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Suggestion does not exist!")
                    .WithDescription($"Cound not fetch suggestion from tracker / message ID / staff message ID `{Tracker}`.\n" +
                    $"Are you sure it exists?")
                    .SendEmbed(Context.Channel);
            else
                await SuggestionService.BuildSuggestion(Suggestion)
                    .SendEmbed(Context.Channel);
        }

    }
}