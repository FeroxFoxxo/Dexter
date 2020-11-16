using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Warnings;
using Discord;
using Discord.Commands;
using Discord.Net;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class WarningCommands {

        /// <summary>
        /// The Issue Warning method runs on the WARN command. It applies a warning to a user by adding it to the related database.
        /// It attaches this warning with a reason, and then notifies the recipient of the warning having been applied.
        /// This command can only be used by a moderator or higher position in the server.
        /// </summary>
        /// <param name="User">The user of which you wish to warn.</param>
        /// <param name="Reason">The reason for the user having been warned.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        [Command("warn")]
        [Summary("Issues a warning to a specified user.")]
        [Alias("issue", "warnUser")]
        [RequireModerator]

        public async Task IssueWarning(IUser User, [Remainder] string Reason) {
            if (Reason.Length > 250) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Reason too long")
                    .WithDescription($"Your reason should be around 250 characters, give or take-\nWe found {Reason.Length} instead.")
                    .SendEmbed(Context.Channel);
                return;
            }

            int WarningID = WarningsDB.Warnings.Count() + 1;

            int TotalWarnings = WarningsDB.GetWarnings(User.Id).Length + 1;

            WarningsDB.Warnings.Add(new Warning() {
                Issuer = Context.Message.Author.Id,
                Reason = Reason,
                User = User.Id,
                WarningID = WarningID,
                WarningType = WarningType.Issued,
                TimeOfIssue = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            await WarningsDB.SaveChangesAsync();

            EmbedBuilder Embed = BuildEmbed(EmojiEnum.Love)
                .WithTitle("Warning Issued!")
                .WithDescription($"Warned {User.GetUserInformation()}")
                .AddField("Issued by", Context.Message.Author.GetUserInformation())
                .AddField("Warning ID", WarningID)
                .AddField("Total Warnings", TotalWarnings)
                .AddField("Reason", Reason.Length > 250 ? Reason.Substring(0, 250) + "..." : Reason)
                .WithCurrentTimestamp();

            try {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"You were issued a warning from {Context.Guild.Name}")
                    .WithDescription("Please note that whenever we warn you verbally, you will recieve a logged warning. " +
                    "This is not indicative of a mute or ban.")
                    .AddField("Reason", Reason)
                    .AddField("Total Warnings", TotalWarnings)
                    .WithCurrentTimestamp()
                    .SendEmbed(User);

                Embed.AddField("Success", "The DM was successfully sent!");
            } catch (HttpException) {
                Embed.AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
            }

            await Embed.SendEmbed(Context.Channel);
        }

    }
}
