using System;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.EventTimers;
using Dexter.Databases.FinalWarns;
using Dexter.Databases.Infractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Humanizer;

namespace Dexter.Commands
{
    partial class ModeratorCommands
    {

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

        public async Task IssueFinalWarn(IGuildUser User, TimeSpan MuteDuration, [Remainder] string Reason)
        {
            short PointsDeducted = ModerationConfiguration.FinalWarningPointsDeducted;

            if (ModerationService.IsUserFinalWarned(User))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User is already final warned!")
                    .WithDescription($"The target user, {User.GetUserInformation()}, already has an active final warn. If you wish to overwrite this final warn, first remove the already existing one.")
                    .SendEmbed(Context.Channel);

                return;
            }

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User.Id);

            DexterProfile.InfractionAmount -= PointsDeducted;

            if (!TimerService.TimerExists(DexterProfile.CurrentPointTimer))
                DexterProfile.CurrentPointTimer = await CreateEventTimer(ModerationService.IncrementPoints, new() { { "UserID", User.Id.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);


            ulong WarningLogID = 0;

            if (ModerationConfiguration.FinalWarningsManageRecords)
            {
                WarningLogID = (await (DiscordShardedClient.GetChannel(ModerationConfiguration.FinalWarningsChannelID) as ITextChannel).SendMessageAsync(
                    $"**Final Warning Issued >>>** <@&{BotConfiguration.ModeratorRoleID}>\n" +
                    $"**User**: {User.GetUserInformation()}\n" +
                    $"**Issued on**: {DateTime.Now:MM/dd/yyyy}\n" +
                    $"**Reason**: {Reason}")).Id;
            }

            SetOrCreateFinalWarn(PointsDeducted, Context.User as IGuildUser, User, MuteDuration, Reason, WarningLogID);

            await ModerationService.MuteUser(User, MuteDuration);

            try
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"🚨 You were issued a **FINAL WARNING** from {Context.Guild.Name}! 🚨")
                    .WithDescription(Reason)
                    .AddField("Points Deducted:", PointsDeducted, true)
                    .AddField("Mute Duration:", MuteDuration.Humanize(), true)

                    .SendEmbed(await User.CreateDMChannelAsync());

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Message sent successfully!")
                    .WithDescription($"The target user, {User.GetUserInformation()}, has been informed of their current status.")
                    .AddField("Mute Duration:", MuteDuration.Humanize(), true)
                    .AddField("Points Deducted:", PointsDeducted, true)
                    .AddField("Issued By:", Context.User.GetUserInformation())
                    .AddField("Reason:", Reason)
                    .SendEmbed(Context.Channel);
            }
            catch (HttpException)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Message failed!")
                    .WithDescription($"The target user, {User.GetUserInformation()}, might have DMs disabled or might have blocked me... :c\nThe final warning has been recorded to the database regardless.")
                    .SendEmbed(Context.Channel);
            }
            await InfractionsDB.SaveChangesAsync();
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

        public async Task RevokeFinalWarn(IGuildUser User, [Remainder] string Reason = "")
        {
            if (!TryRevokeFinalWarn(User, out FinalWarn Warn))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("No active final warn found!")
                    .WithDescription($"Wasn't able to revoke final warning for user {User.GetUserInformation()}, since no active warn exists.")
                    .SendEmbed(Context.Channel);

                return;
            }

            if (Warn.MessageID != 0)
                await (await (DiscordShardedClient.GetChannel(ModerationConfiguration.FinalWarningsChannelID) as ITextChannel).GetMessageAsync(Warn.MessageID))?.DeleteAsync();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Final warn successfully revoked.")
                .WithDescription($"Successfully revoked final warning for user {User.GetUserInformation()}. You can still query records about this final warning.")
                .AddField(Reason.Length > 0, "Reason:", Reason)
                .SendEmbed(Context.Channel);

            try
            {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Your final warning has been revoked!")
                    .WithDescription("The staff team has convened and decided to revoke your final warning. Be careful, you can't receive more than two final warnings! A third one is an automatic ban.")
                    .AddField(Reason.Length > 0, "Reason:", Reason)
                    .SendEmbed(await User.CreateDMChannelAsync());
            }
            catch (HttpException)
            {
                await Context.Channel.SendMessageAsync("This user either has closed DMs or has me blocked! I wasn't able to inform them of this.");
            }
            await InfractionsDB.SaveChangesAsync();
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

        public async Task GetFinalWarn(IGuildUser User)
        {
            FinalWarn Warn = FinalWarnsDB.FinalWarns.Find(User.Id);

            if (Warn == null)
            {
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
                .AddField("Issued by:", DiscordShardedClient.GetUser(Warn.IssuerID).GetUserInformation())
                .AddField("Mute Duration:", TimeSpan.FromSeconds(Warn.MuteDuration).Humanize(), true)
                .AddField("Points Deducted:", Warn.PointsDeducted, true)
                .AddField("Issued on:", DateTimeOffset.FromUnixTimeSeconds(Warn.IssueTime).Humanize(), true)
                .SendEmbed(Context.Channel);
            await InfractionsDB.SaveChangesAsync();
        }
        /// <summary>
        /// Looks for a final warn entry for the target <paramref name="user"/>. If none are found, it creates one. If one is found, it is overwritten by the new parameters.
        /// </summary>
        /// <param name="deducted">The amounts of points deducted from the <paramref name="user"/>'s profile when the final warn was issued.</param>
        /// <param name="issuer">The staff member who issued the final warning.</param>
        /// <param name="user">The user who is to receive a final warn.</param>
        /// <param name="duration">The duration of the mute attached to the final warn.</param>
        /// <param name="reason">The whole reason behind the final warn for <paramref name="user"/>.</param>
        /// <param name="msgID">The ID of the message within #final-warnings which records this final warn instance.</param>
        /// <returns>The <c>FinalWarn</c> object added to the database.</returns>

        public FinalWarn SetOrCreateFinalWarn(short deducted, IGuildUser issuer, IGuildUser user, TimeSpan duration, string reason, ulong msgID)
        {
            FinalWarn warning = FinalWarnsDB.FinalWarns.Find(user.Id);

            FinalWarn newWarning = new()
            {
                IssuerID = issuer.Id,
                UserID = user.Id,
                MuteDuration = duration.TotalSeconds,
                Reason = reason,
                EntryType = EntryType.Issue,
                IssueTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                MessageID = msgID,
                PointsDeducted = deducted
            };

            if (warning == null)
            {
                FinalWarnsDB.FinalWarns.Add(newWarning);
            }
            else
            {
                FinalWarnsDB.FinalWarns.Remove(warning);
                FinalWarnsDB.FinalWarns.Add(newWarning);
            }

            InfractionsDB.SaveChanges();

            return newWarning;
        }

        /// <summary>
        /// Sets the status of a <paramref name="user"/>'s final warn to Revoked, but doesn't remove it from the database.
        /// </summary>
        /// <param name="user">The user whose final warn is to be revoked.</param>
        /// <param name="warning">The warn found in the database, or <see langword="null"/> if no warn is found.</param>
        /// <returns><see langword="true"/> if an active final warn was found for the <paramref name="user"/>, whose status was changed to revoked; otherwise <see langword="false"/>.</returns>

        public bool TryRevokeFinalWarn(IGuildUser user, out FinalWarn warning)
        {
            warning = FinalWarnsDB.FinalWarns.Find(user.Id);

            if (warning == null || warning.EntryType == EntryType.Revoke) return false;

            warning.EntryType = EntryType.Revoke;

            InfractionsDB.SaveChanges();

            return true;
        }

    }
}
