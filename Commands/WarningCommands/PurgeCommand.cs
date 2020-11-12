using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Warnings;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class WarningCommands {

        [Command("purgewarns")]
        [Summary("Removes all warnings from a user [CONFIRMATION]")]
        [RequireAdministrator]

        public async Task PurgeWarningsCommand([Remainder] string Token) {
            if (WarningsDB.PurgeConfirmations.AsQueryable().Where(Purge => Purge.Token == Token).FirstOrDefault() == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Invalid Purge Confirmation")
                    .WithDescription($"Whoops! It seems as though the purge confirmation token `{Token}` does not exist in the confirmation logs! " +
                    $"Please triple check this is the right token <3")
                    .SendEmbed(Context.Channel);

                return;
            }

            PurgeConfirmation PurgeConfirmation = WarningsDB.PurgeConfirmations.AsQueryable().Where(Purge => Purge.Token == Token).FirstOrDefault();

            WarningsDB.PurgeConfirmations.Remove(PurgeConfirmation);

            int Count = WarningsDB.GetWarnings(PurgeConfirmation.User).Length;

            await WarningsDB.Warnings.AsQueryable().Where(Warning => Warning.User == PurgeConfirmation.User).ForEachAsync(Warning => Warning.Type = WarningType.Revoked);

            await WarningsDB.SaveChangesAsync();

            SocketGuildUser User = Context.Guild.GetUser(PurgeConfirmation.User);

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Warnings Purged")
                .WithDescription($"Heya! I've purged {Count} warnings from {(User == null ? "Unknown" : User.GetUserInformation())}.")
                .AddField("Purged by", Context.Message.Author.GetUserInformation())
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

        [Command("purgewarns")]
        [Summary("Removes all warnings from a user.")]
        [RequireAdministrator]

        public async Task PurgeWarningsCommand(IUser User) {
            if (WarningsDB.PurgeConfirmations.AsQueryable().Where(Purge => Purge.User == User.Id).FirstOrDefault() != null) {
                string Token = WarningsDB.PurgeConfirmations.AsQueryable().Where(Purge => Purge.User == User.Id).Select(Purge => Purge.Token).FirstOrDefault();

                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Token already generated!")
                    .WithDescription($"{User.GetUserInformation()} already has a confirmation token of `{Token}`. " +
                        $"Please re-enter this token using the command `{BotConfiguration.Prefix}purgewarns {Token}`.")
                    .SendEmbed(Context.Channel);
                return;
            }

            string TokenString = Token();

            WarningsDB.PurgeConfirmations.Add(new PurgeConfirmation() {
                Token = TokenString,
                User = User.Id
            });

            await WarningsDB.SaveChangesAsync();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Confirmation Required")
                .WithDescription($"Please reissue this command with the token of `{TokenString}` in order to confirm that you wish to purge the warnings for {User.GetUserInformation()} <3")
                .SendEmbed(Context.Channel);
        }

    }
}
