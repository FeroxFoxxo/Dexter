using Dexter.Core.Attributes;
using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Dexter.Databases.Suggestions;
using Dexter.Services;
using Discord;
using Discord.Commands;
using Discord.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class SuggestionCommands {

        [Command("decline")]
        [Summary("Denies a suggestion from an ID with an optional reason.")]
        [Alias("deny")]
        [RequireAdministrator]

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

                EmbedBuilder Builder = Context.BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Suggestion Denied")
                    .WithDescription($"Suggestion {Suggestion.TrackerID} was successfully denied by {Context.Message.Author.Mention}")
                    .AddField("Reason:", string.IsNullOrEmpty(Reason) ? "No reason provided" : Reason)
                    .WithCurrentTimestamp();

                try {
                    await SuggestionService.BuildSuggestion(Suggestion).SendEmbed(Context.Guild.GetUser(Suggestion.Suggestor));

                    Builder.AddField("Success", "The DM was successfully sent!");
                } catch (HttpException) {
                    Builder.AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
                }

                await Builder.SendEmbed(Context.Channel);
            }
        }

    }
}
