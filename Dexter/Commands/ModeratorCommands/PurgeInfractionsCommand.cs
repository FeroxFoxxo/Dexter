using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Infractions;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Dexter.Attributes.Methods;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// The Purge Infractions method runs on PURGEWARNS. It sends a callback to the SendForAdminApproval
        /// method, which will put it up to be voted on by the administrators. After this has been voted on, it will
        /// set all warnings to a revoked state and notify the user that their warnings have been removed.
        /// </summary>
        /// <param name="User">The user from which you wish all warnings to be cleared from.</param>
        /// <returns></returns>

        [Command("purgerecords")]
        [Summary("Removes all infractions from a user, having been sent to the admins for confirmation.")]
        [Alias("purgeinfractions")]
        [RequireAdministrator]

        public async Task PurgeInfractions (IUser User) {
            await SendForAdminApproval(PurgeWarningsCallback,
                new Dictionary<string, string>() {
                    { "UserID", User.Id.ToString() }
                },
                Context.User.Id,
                $"{Context.User.Username} has proposed that the user {User.GetUserInformation()} should have all their infractions purged. " +
                $"On approval of this command, {User.Username}'s slate will be fully cleared."
            );

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Infractions Purge Confimation")
                .WithDescription($"Heya! I've send the infractions purge for {User.GetUserInformation()} to the administrators for approval.")
                .AddField("Purge instantiated by", Context.User.GetUserInformation())
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// The PurgeWarningsCallback fires on an admin approving a purge confirmation. It sets all warnings to a revoked state.
        /// </summary>
        /// <param name="CallbackInformation">The callback information is a dictionary of parameters parsed to the original callback statement.
        ///     UserID = Specifies the UserID who will have their warnings purged.
        /// </param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async void PurgeWarningsCallback(Dictionary<string, string> CallbackInformation) {
            ulong UserID = Convert.ToUInt64(CallbackInformation["UserID"]);

            int Count = InfractionsDB.GetInfractions(UserID).Length;

            await InfractionsDB.Infractions.AsQueryable().Where(Warning => Warning.User == UserID).ForEachAsync(Warning => Warning.EntryType = EntryType.Revoke);

            InfractionsDB.SaveChanges();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Infractions Purged")
                .WithDescription($"Heya! I've purged {Count} warnings from your account. You now have a clean slate! <3")
                .WithCurrentTimestamp()
                .SendEmbed(await Client.GetUser(UserID).GetOrCreateDMChannelAsync());
        }

    }

}
