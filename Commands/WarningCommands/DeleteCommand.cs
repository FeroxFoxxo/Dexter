using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Warnings;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class WarningCommands {

        [Command("delwarn")]
        [Summary("Removes a warning from a specified user.")]
        [Alias("deletewarn", "revokewarn")]
        [RequireModerator]

        public async Task DeleteWarningCommand(int WarningID) {
            Warning Warning = WarningsDB.Warnings.AsQueryable().Where(Warning => Warning.WarningID == WarningID).FirstOrDefault();

            Warning.Type = WarningType.Revoked;

            await WarningsDB.SaveChangesAsync();

            SocketGuildUser Issuer = Context.Guild.GetUser(Warning.Issuer);
            SocketGuildUser Warned = Context.Guild.GetUser(Warning.User);

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Warning revoked!")
                .WithDescription($"Heya! I revoked a warning issued to {(Warned == null ? "Unknown" : Warned.GetUserInformation())}")
                .AddField("Issued by", Issuer == null ? "Unknown" : Issuer.GetUserInformation())
                .AddField("Revoked by", Context.Message.Author.GetUserInformation())
                .AddField("Reason", Warning.Reason)
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

    }
}