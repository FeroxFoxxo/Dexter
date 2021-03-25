using Dexter.Attributes.Methods;
using Dexter.Databases.EventTimers;
using Dexter.Databases.FinalWarns;
using Dexter.Databases.Infractions;
using Dexter.Services;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Humanizer;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {
    partial class ModeratorCommands {

        /// <summary>
        /// Issues a final warning to a target <paramref name="User"/>, mutes then for <paramref name="MuteDuration"/>, and adds a detailed entry about the final warn to the Final Warns database.
        /// </summary>
        /// <param name="User">The target user to final warn.</param>
        /// <param name="MuteDuration">The duration of the mute attached to the final warn.</param>
        /// <param name="Reason">The reason behind the final warn.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("finalwarn")]
        [Summary("Issues a final warning to a user, mutes them and records the final warn.")]
        [Alias("warnfinal")]
        [RequireModerator]
        [BotChannel]

        public async Task IssueFinalWarn(IGuildUser User, TimeSpan MuteDuration, [Remainder] string Reason) {
            short PointsDeducted = ModerationConfiguration.FinalWarningPointsDeducted;

            if (FinalWarnsDB.IsUserFinalWarned(User)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User is already final warned!")
                    .WithDescription($"The target user, {User.GetUserInformation()}, already has an active final warn. If you wish to overwrite this final warn, first remove the already existing one.")
                    .SendEmbed(Context.Channel);

                return;
            }

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User.Id);

            DexterProfile.InfractionAmount -= PointsDeducted;

            if (!TimerService.TimerExists(DexterProfile.CurrentPointTimer))
                DexterProfile.CurrentPointTimer = await CreateEventTimer(IncrementPoints, new() { { "UserID", User.Id.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);


            ulong WarningLogID = 0;

            if (ModerationConfiguration.FinalWarningsManageRecords) {
                WarningLogID = (await (DiscordSocketClient.GetChannel(ModerationConfiguration.FinalWarningsChannelID) as ITextChannel).SendMessageAsync(
                    $"**Final Warning Issued >>>** <@&{BotConfiguration.ModeratorRoleID}>\n" +
                    $"**User**: {User.GetUserInformation()}\n" +
                    $"**Issued on**: {DateTime.Now:MM/dd/yyyy}\n" +
                    $"**Reason**: {Reason}")).Id;
            }

            FinalWarnsDB.SetOrCreateFinalWarn(PointsDeducted, Context.User as IGuildUser, User, MuteDuration, Reason, WarningLogID);

            await MuteUser(User, MuteDuration);

            try {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"🚨 You were issued a **FINAL WARNING** from {Context.Guild.Name}! 🚨")
                    .WithDescription(Reason)
                    .AddField("Points Deducted:", PointsDeducted, true)
                    .AddField("Mute Duration:", MuteDuration.Humanize(), true)
                    .WithCurrentTimestamp()
                    .SendEmbed(await User.GetOrCreateDMChannelAsync());

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Message sent successfully!")
                    .WithDescription($"The target user, {User.GetUserInformation()}, has been informed of their current status.")
                    .AddField("Mute Duration:", MuteDuration.Humanize(), true)
                    .AddField("Points Deducted:", PointsDeducted, true)
                    .AddField("Issued By:", Context.User.GetUserInformation())
                    .AddField("Reason:", Reason)
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Channel);
            }
            catch (HttpException) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Message failed!")
                    .WithDescription($"The target user, {User.GetUserInformation()}, might have DMs disabled or might have blocked me... :c\nThe final warning has been recorded to the database regardless.")
                    .SendEmbed(Context.Channel);
            }

            InfractionsDB.SaveChanges();
        }

        /// <summary>
        /// Revokes a final warning if an active one exists for target <paramref name="User"/>, and informs them of this.
        /// </summary>
        /// <param name="User">The user whose final warn is to be revoked.</param>
        /// <param name="Reason">The reason why the final warn was revoked. (Optional)</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("delfinalwarn")]
        [Summary("Revokes a user's final warn, though it remains in records.")]
        [Alias("revokefinalwarn", "deletefinalwarn", "removefinalwarn")]
        [RequireModerator]
        [BotChannel]

        public async Task RevokeFinalWarn(IGuildUser User, [Remainder] string Reason = "") {
            
            if(!FinalWarnsDB.TryRevokeFinalWarn(User, out FinalWarn Warn)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("No active final warn found!")
                    .WithDescription($"Wasn't able to revoke final warning for user {User.GetUserInformation()}, since no active warn exists.")
                    .SendEmbed(Context.Channel);

                return;
            }

            if(Warn.MessageID != 0)
                await (await (DiscordSocketClient.GetChannel(ModerationConfiguration.FinalWarningsChannelID) as ITextChannel).GetMessageAsync(Warn.MessageID))?.DeleteAsync();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Final warn successfully revoked.")
                .WithDescription($"Successfully revoked final warning for user {User.GetUserInformation()}. You can still query records about this final warning.")
                .AddField(Reason.Length > 0, "Reason:", Reason)
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);

            try {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Your final warning has been revoked!")
                    .WithDescription("The staff team has convened and decided to revoke your final warning. Be careful, you can't receive more than two final warnings! A third one is an automatic ban.")
                    .AddField(Reason.Length > 0, "Reason:", Reason)
                    .WithCurrentTimestamp()
                    .SendEmbed(await User.GetOrCreateDMChannelAsync());
            }
            catch(HttpException) {
                await Context.Channel.SendMessageAsync("This user either has closed DMs or has me blocked! I wasn't able to inform them of this.");
            }
        }

        /// <summary>
        /// Gets the information for a final warn attached to <paramref name="User"/>, if any.
        /// </summary>
        /// <param name="User">The user to query for in the final warnings database.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("getfinalwarn")]
        [Summary("Gets the relevant information for a user's final warn.")]
        [Alias("queryfinalwarn")]
        [RequireModerator]
        [BotChannel]

        public async Task GetFinalWarn(IGuildUser User) {
            FinalWarn Warn = FinalWarnsDB.FinalWarns.Find(User.Id);

            if (Warn == null) {
                await BuildEmbed(EmojiEnum.Wut)
                    .WithTitle("Target user is not under a final warning!")
                    .WithDescription($"User {User.GetUserInformation()} has no final warnings to their name!")
                    .SendEmbed(Context.Channel);

                return;
            }

            await BuildEmbed(EmojiEnum.Sign)
                .WithTitle("Final warning found!")
                .WithDescription($"User {User.GetUserInformation()} has {(Warn.EntryType == EntryType.Revoke ? "a **revoked**" : "an **active**")} final warning!")
                .AddField("Reason:", Warn.Reason)
                .AddField("Issued by:", DiscordSocketClient.GetUser(Warn.IssuerID).GetUserInformation())
                .AddField("Mute Duration:", TimeSpan.FromSeconds(Warn.MuteDuration).Humanize(), true)
                .AddField("Points Deducted:", Warn.PointsDeducted, true)
                .AddField("Issued on:", DateTimeOffset.FromUnixTimeSeconds(Warn.IssueTime).Humanize(), true)
                .SendEmbed(Context.Channel);
        }
    }
}
