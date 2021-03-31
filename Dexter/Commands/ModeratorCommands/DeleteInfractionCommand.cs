using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Infractions;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// The Delete Infraction method runs on UNMUTE. It sets a record to a revoked status,
        /// making it so that the record is thus removed from an individual and cannot be seen through the records command.
        /// </summary>
        /// <param name="User">The ID user you want the infraction removed from.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("unmute")]
        [Summary("Removes an infraction from a specified user based on the infraction's ID.")]
        [Alias("delmute")]
        [RequireModerator]

        public async Task DeleteInfraction(IGuildUser User) {
            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User.Id);

            DexterProfile.CurrentPointTimer = string.Empty;

            await RemoveMutedRole(new() { { "UserID", User.Id.ToString() } });

            InfractionsDB.SaveChanges();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Successfully Unmuted {User.Username}.")
                .WithDescription($"Heya! I have successfully unmuted {User.GetUserInformation()}. Give them a headpat. <3")
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// The Delete Infraction method runs on DELRECORD. It sets a record to a revoked status,
        /// making it so that the record is thus removed from an individual and cannot be seen through the records command.
        /// </summary>
        /// <param name="InfractionID">The ID of the infraction that you wish to remove from the user.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("delrecord")]
        [Summary("Removes an infraction from a specified user based on the infraction's ID.")]
        [Alias("unmute", "unwarn", "delwarn", "delmute")]
        [RequireModerator]

        public async Task DeleteInfraction (int InfractionID) {
            Infraction Infraction = InfractionsDB.Infractions.Find(InfractionID);

            Infraction.EntryType = EntryType.Revoke;

            SocketGuildUser Issuer = Context.Guild.GetUser(Infraction.Issuer);
            SocketGuildUser Warned = Context.Guild.GetUser(Infraction.User);

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(Infraction.User);

            DexterProfile.InfractionAmount += Infraction.PointCost;

            if (DexterProfile.InfractionAmount > ModerationConfiguration.MaxPoints)
                DexterProfile.InfractionAmount = ModerationConfiguration.MaxPoints;

            if (Infraction.PointCost > 2)
                await RemoveMutedRole(new() { { "UserID", Infraction.User.ToString() } });

            InfractionsDB.SaveChanges();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Infraction Revoked! New Points: {DexterProfile.InfractionAmount}.")
                .WithDescription($"Heya! I revoked an infraction issued from {(Warned == null ? $"Unknown ({Infraction.User})" : Warned.GetUserInformation())}")
                .AddField("Issued by", Issuer == null ? $"Unknown ({Infraction.Issuer})" : Issuer.GetUserInformation())
                .AddField("Revoked by", Context.User.GetUserInformation())
                .AddField("Reason", Infraction.Reason)
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

    }

}