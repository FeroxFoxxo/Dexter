using Dexter.Abstractions;
using Dexter.Databases.Suggestions;
using Dexter.Services;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands.SuggestionCommands {
    public partial class SuggestionCommands {

        [Command("decline")]
        [Summary("Denies a suggestion from an ID with an optional reason.")]
        [Alias("deny")]

        public async Task DenySuggestion(string Tracker, [Optional][Remainder] string Reason) {
            Suggestion Suggestion = SuggestionDB.GetSuggestionByNameOrID(Tracker);

            if (Suggestion == null)
                await Context.BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Suggestion does not exist!")
                    .WithDescription($"Cound not fetch suggestion from tracker / message ID / staff message ID `{Tracker}`.\n" +
                    $"Are you sure it exists?")
                    .SendEmbed(Context.Channel);
            else {
                Suggestion.Reason = Reason;

                await SuggestionService.UpdateSuggestion(Suggestion, SuggestionStatus.Declined);

                await Context.BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Suggestion Denied")
                    .WithDescription($"Suggestion {Suggestion.TrackerID} was successfully approved by {Context.Message.Author.Mention}")
                    .AddField("Reason:", string.IsNullOrEmpty(Reason) ? "No reason provided" : Reason)
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Channel);
            }
        }

    }
}
