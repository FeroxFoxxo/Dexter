using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Warnings;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Discord.Net;

namespace Dexter.Commands {
    public partial class WarningCommands {

        /// <summary>
        /// The Purge Warnings method runs on the PURGEWARNS command. It sends a callback to the SendForAdminApproval
        /// method, which will put it up to be voted on by the administrators. After this has been voted on, it will
        /// set all warnings to a revoked state and notify the user that their warnings have been removed.
        /// </summary>
        /// <param name="User">The user from which you wish all warnings to be cleared from.</param>
        /// <returns></returns>

        [Command("purgewarns")]
        [Summary("Removes all warnings from a user.")]

        public async Task PurgeWarnings(IUser User) {
            await SendForAdminApproval(PurgeWarningsCallback,
                new Dictionary<string, string>() {
                    { "UserID", User.Id.ToString() }
                },
                Context.Message.Author.Id,
                $"{Context.Message.Author.Username} has proposed that the user {User.GetUserInformation()} should have all their warnings purged. " +
                $"On approval of this command, {User.Username}'s slate will be fully cleared."
            );

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Warnings Purge Confimation")
                .WithDescription($"Heya! I've send the warnings purge for {User.GetUserInformation()} to the administrators for approval.")
                .AddField("Purge instantiated by", Context.Message.Author.GetUserInformation())
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// The PurgeWarningsCallback fires on an admin approving a purge confirmation. It sets all warnings to a revoked state.
        /// </summary>
        /// <param name="CallbackInformation">The callback information is a dictionary of parameters parsed to the original callback statement.
        ///     UserID = Specifies the UserID who will have their warnings purged.
        /// </param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        
        public async Task PurgeWarningsCallback(Dictionary<string, string> CallbackInformation) {
            ulong UserID = Convert.ToUInt64(CallbackInformation["UserID"]);

            int Count = WarningsDB.GetWarnings(UserID).Length;

            await WarningsDB.Warnings.AsQueryable().Where(Warning => Warning.User == UserID).ForEachAsync(Warning => Warning.Type = WarningType.Revoked);

            await WarningsDB.SaveChangesAsync();

            try {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Warnings Purged")
                    .WithDescription($"Heya! I've purged {Count} warnings from your account. You now have a clean slate! <3")
                    .WithCurrentTimestamp()
                    .SendEmbed(Client.GetUser(UserID));
            } catch (HttpException) { }
        }

    }
}
