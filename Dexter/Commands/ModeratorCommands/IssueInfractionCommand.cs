using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Infractions;
using Discord;
using Discord.Commands;
using Discord.Net;
using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Dexter.Databases.EventTimers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// The Issue Infraction method runs on WARN. It applies a warning to a user by adding it to the related database.
        /// It attaches this warning with a reason, and then notifies the recipient of the warning having been applied.
        /// This command can only be used by a moderator or higher position in the server.
        /// </summary>
        /// <param name="PointsDeducted">The number of points to deduct to the user's Dexter Profile for automoderation purposes.</param>
        /// <param name="User">The user of which you wish to warn.</param>
        /// <param name="Reason">The reason for the user having been warned.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        [Command("warn")]
        [Summary("Issues a warning to a specified user.")]
        [Alias("warnUser")]
        [RequireModerator]

        public async Task IssueWarning(short PointsDeducted, IGuildUser User, [Remainder] string Reason) {
            if (PointsDeducted == 1 || PointsDeducted == 2)
                await IssueInfraction(PointsDeducted, User, TimeSpan.FromSeconds(0), Reason);
            else
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Incorrect Infraction Type.")
                    .WithDescription($"Haiya! All warning commands should either have 1 or 2 points deducted. What we found was {PointsDeducted} {(PointsDeducted == 1 ? "point" : "points")}. <3")
                    .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// The Issue Mute method runs on MUTE. It applies a mute to a user by adding it to the related database.
        /// It attaches this warning with a reason, and then notifies the recipient of the warning having been applied.
        /// This command can only be used by a moderator or higher position in the server and will mute the user for the set time.
        /// </summary>
        /// <param name="PointsDeducted">The number of points to subtract from the target user's Dexter Profile for automoderation purposes. Must be 0, 3 or 4.</param>
        /// <param name="User">The user of which you wish to mute.</param>
        /// <param name="Time">The duration of the mute.</param>
        /// <param name="Reason">The reason for the user having been mute.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        [Command("mute")]
        [Summary("Issues a mute to a specified user.")]
        [Alias("muteUser")]
        [RequireModerator]

        public async Task IssueMute(short PointsDeducted, IGuildUser User, TimeSpan Time, [Remainder] string Reason) {
            if (PointsDeducted is 3 or 4 or 0)
                await IssueInfraction(PointsDeducted, User, Time, Reason);
            else
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Incorrect Infraction Type.")
                    .WithDescription($"Haiya! All mute commands should either have 3 or 4 points deducted. What we found was {PointsDeducted} {(PointsDeducted == 1 ? "point" : "points")}. <3")
                    .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Issues a mute to a specified user but does not add it to their records or affect their score.
        /// </summary>
        /// <remarks>This command should be used to issue non-punitive mutes only.</remarks>
        /// <param name="User"></param>
        /// <param name="Time"></param>
        /// <param name="Reason"></param>
        /// <returns>A <c>Task</c> object, which we can await until this method completes successfully.</returns>

        [Command("mute")]
        [Summary("Issues a mute to a specified user. Does not add it to their records.")]
        [Alias("muteUser")]
        [RequireModerator]

        public async Task IssueMute(IGuildUser User, TimeSpan Time, [Remainder] string Reason) {
            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User.Id);

            if (!string.IsNullOrEmpty(DexterProfile.CurrentMute))
                TimerService.RemoveTimer(DexterProfile.CurrentMute);

            IRole Role = Context.Guild.GetRole(ModerationConfiguration.MutedRoleID);

            if (!User.RoleIds.Contains(ModerationConfiguration.MutedRoleID))
                await User.AddRoleAsync(Role);

            DexterProfile.CurrentMute = await CreateEventTimer(RemoveMutedRole, new() { { "UserID", User.Id.ToString() } }, Convert.ToInt32(Time.TotalSeconds), TimerType.Expire);

            EmbedBuilder Embed = BuildEmbed(EmojiEnum.Love)
                .WithTitle("Unrecorded Mute Issued!")
                .WithDescription($"Warned {User.GetUserInformation()} for **{Time.Humanize(2)}** due to `{Reason}`. Please note that as this mute has not had a point count attached to it, it has not been recorded.");
            
            try {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"Unrecorded Mute Applied!")
                    .WithDescription($"You have been muted in `{Context.Guild.Name}` for `{Reason}` for a time of `{Time.Humanize(2)}`. We hope you enjoy your time. <3")
                    .WithCurrentTimestamp()
                    .SendEmbed(await User.GetOrCreateDMChannelAsync());

                Embed.AddField("Success", $"The user has been successfully alerted of this unrecorded mute!");
            } catch (HttpException) {
                Embed.AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
            }

            await Embed.SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Issues an indefinite mute to a specified user.
        /// </summary>
        /// <param name="PointsDeducted">The number of points to remove from the user's Dexter profile. Must be set to 0.</param>
        /// <param name="User">Target user to mute.</param>
        /// <param name="Reason">A string description of the reason why the mute was issued.</param>
        /// <returns>A <c>Task</c> object, which we can await until this method completes successfully.</returns>

        [Command("mute")]
        [Summary("Issues an infinite mute to a specified user given the point amount of 0.")]
        [Alias("muteUser")]
        [RequireModerator]

        public async Task IssueMute(short PointsDeducted, IGuildUser User, [Remainder] string Reason) {
            if (PointsDeducted == 0)
                await IssueInfraction(PointsDeducted, User, TimeSpan.FromSeconds(0), Reason);
            else
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Incorrect Infraction Type.")
                    .WithDescription($"Haiya! Infinite mutes should have 0 points deducted. What we found was {PointsDeducted} {(PointsDeducted == 1 ? "point" : "points")}. <3")
                    .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Mutes a user for a specific amount of time and removes a specified number of points from their Dexter Profile.
        /// This mute is recorded in the corresponding database.
        /// </summary>
        /// <param name="PointsDeducted">The amount of points to remove from the user's Dexter Profile for automoderation purposes.</param>
        /// <param name="User">The target user.</param>
        /// <param name="Time">The duration of the mute.</param>
        /// <param name="Reason">A string description of the reason why the mute was issued.</param>
        /// <returns>A <c>Task</c> object, which we can await until this method completes successfully.</returns>

        public async Task IssueInfraction(short PointsDeducted, IGuildUser User, TimeSpan Time, [Remainder] string Reason) {
            if (Reason.Length > 250) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Reason too long.")
                    .WithDescription($"Your reason should be at most 250 characters long. \nWe found {Reason.Length} instead.")
                    .SendEmbed(Context.Channel);
            }

            int InfractionID = InfractionsDB.Infractions.Any() ? InfractionsDB.Infractions.Max(Warning => Warning.InfractionID) + 1 : 1;

            int TotalInfractions = InfractionsDB.GetInfractions(User.Id).Length + 1;

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User.Id);

            if (DexterProfile.InfractionAmount - PointsDeducted <= -4 && DexterProfile.InfractionAmount > -4)
                await BuildEmbed(EmojiEnum.Wut)
                    .WithTitle($"Frequent Rulebreaker Inbound!!!")
                    .WithDescription($"Haiya! It seems as though the user {User.GetUserInformation()} is currently standing on {DexterProfile.InfractionAmount - PointsDeducted}. Perhaps this is something the {ModerationConfiguration.AdministratorMention}s can dwell on. <3")
                    .WithCurrentTimestamp()
                    .SendEmbed(DiscordSocketClient.GetChannel(BotConfiguration.ModerationLogChannelID) as ITextChannel);

            DexterProfile.InfractionAmount -= PointsDeducted;

            TimeSpan? AdditionalTime = null;

            if (DexterProfile.InfractionAmount == 0)
                AdditionalTime = TimeSpan.FromMinutes(30);
            else if (DexterProfile.InfractionAmount == -1)
                AdditionalTime = TimeSpan.FromMinutes(45);
            else if (DexterProfile.InfractionAmount == -2)
                AdditionalTime = TimeSpan.FromHours(1);
            else if (DexterProfile.InfractionAmount == -3)
                AdditionalTime = TimeSpan.FromHours(2);
            else if (DexterProfile.InfractionAmount <= -4)
                AdditionalTime = TimeSpan.FromHours(3);

            if (AdditionalTime.HasValue) {
                Time = Time.Add(AdditionalTime.Value);

                Reason += $"\n**Automatic mute of {AdditionalTime.Value.Humanize(2)} applied in addition by Dexter for frequent warnings and/or rulebreaks.**";
            }

            if (Time.TotalSeconds > 0) {
                if (!string.IsNullOrEmpty(DexterProfile.CurrentMute))
                    TimerService.RemoveTimer(DexterProfile.CurrentMute);

                IRole Role = Context.Guild.GetRole(ModerationConfiguration.MutedRoleID);

                if (!User.RoleIds.Contains(ModerationConfiguration.MutedRoleID))
                    await User.AddRoleAsync(Role);

                DexterProfile.CurrentMute = await CreateEventTimer(RemoveMutedRole, new() { { "UserID", User.Id.ToString() } }, Convert.ToInt32(Time.TotalSeconds), TimerType.Expire);
            }

            if(PointsDeducted == 0) {
                IRole Role = Context.Guild.GetRole(ModerationConfiguration.MutedRoleID);

                if (!User.RoleIds.Contains(ModerationConfiguration.MutedRoleID))
                    await User.AddRoleAsync(Role);
            }

            if (string.IsNullOrEmpty(DexterProfile.CurrentPointTimer))
                DexterProfile.CurrentPointTimer = await CreateEventTimer(IncrementPoints, new() { { "UserID", User.Id.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);

            InfractionsDB.Infractions.Add(new Infraction() {
                Issuer = Context.User.Id,
                Reason = Reason,
                User = User.Id,
                InfractionID = InfractionID,
                EntryType = EntryType.Issue,
                TimeOfIssue = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                PointCost = PointsDeducted,
                InfrationTime = Time.TotalSeconds
            });

            InfractionsDB.SaveChanges();

            InfractionType InfractionType = PointsDeducted == 0 ? InfractionType.IndefiniteMute : Time.TotalSeconds > 0 ? InfractionType.Mute : InfractionType.Warning;

            EmbedBuilder Embed = null;

            if (Context.Channel.Id == ModerationConfiguration.StaffBotsChannel) {
                Embed = BuildEmbed(EmojiEnum.Love)
                   .WithTitle($"{Regex.Replace(InfractionType.ToString(), "([A-Z])([a-z]*)", " $1$2")} {InfractionID} Issued! Current Points: {DexterProfile.InfractionAmount}.")
                   .WithDescription($"{(InfractionType == InfractionType.Warning ? "Warned" : "Muted")} {User.GetUserInformation()}" +
                       $"{(InfractionType == InfractionType.Mute ? $" for **{Time.Humanize(2)}**" : "")} who currently has **{TotalInfractions} " +
                       $"{(TotalInfractions == 1 ? "infraction" : "infractions")}** and has had **{PointsDeducted} {(PointsDeducted == 1 ? "point" : "points")} deducted.**")
                   .AddField("Issued By", Context.User.GetUserInformation())
                   .AddField(Time.TotalSeconds > 0, "Total Mute Time", $"{Time.Humanize(2)}.")
                   .AddField("Reason", Reason)
                   .WithCurrentTimestamp();
            } else
                Embed = BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"{InfractionType.ToString().Humanize()} issued!")
                    .WithDescription($"{(InfractionType == InfractionType.Warning ? "Warned" : "Muted")} {User.GetUserInformation()} {(InfractionType == InfractionType.Mute ? $" for **{Time.Humanize(2)}**" : "")} due to `{Reason}`");
            try {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"You were issued a {(InfractionType == InfractionType.Warning ? "warning" : $"mute{(InfractionType == InfractionType.Mute ? $" of {Time.Humanize(2)}" : "")}")} from {Context.Guild.Name}.")
                    .WithDescription($"You have had a total of {TotalInfractions} {(TotalInfractions == 1 ? "infraction" : "infractions")} and are on {DexterProfile.InfractionAmount} {(DexterProfile.InfractionAmount == 1 ? "point" : "points")}, you regain one point every 24 hours. " +
                        ( InfractionType == InfractionType.Warning ?
                        "Please note that whenever we warn you verbally, you will recieve a logged warning. This is not indicative of a mute or ban." :
                        $"Please read over the <#{ModerationConfiguration.RulesAndInfoChannel}> channel if you have not already done so, even if it's just for a refresher, as to make sure your behaviour meets the standards of the server. <3")
                    )
                    .AddField("Reason", Reason)
                    .WithCurrentTimestamp()
                    .SendEmbed(await User.GetOrCreateDMChannelAsync());

                Embed.AddField("Success", $"The user has been successfully alerted of the {InfractionType.ToString().Humanize().ToLower()}!");
            } catch (HttpException) {
                Embed.AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
            }

            await Embed.SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Manually adds points to a user's Dexter Profile.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "UserID".
        /// This value should be parsable to a type of <c>ulong</c> (User ID).
        /// </param>
        /// <returns>A <c>Task</c> object, which we can await until this method completes successfully.</returns>

        public async Task IncrementPoints(Dictionary<string, string> Parameters) {
            ulong UserID = ulong.Parse(Parameters["UserID"]);

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(UserID);

            if (DexterProfile.InfractionAmount < ModerationConfiguration.MaxPoints) {
                DexterProfile.InfractionAmount++;
                if (DexterProfile.InfractionAmount < ModerationConfiguration.MaxPoints)
                    await CreateEventTimer(IncrementPoints, new() { { "UserID", UserID.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);
            } else {
                if (DexterProfile.InfractionAmount > ModerationConfiguration.MaxPoints)
                    DexterProfile.InfractionAmount = ModerationConfiguration.MaxPoints;

                DexterProfile.CurrentPointTimer = string.Empty;
            }

            InfractionsDB.SaveChanges();
        }

        /// <summary>
        /// Manually removes points from a user's Dexter Profile.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "UserID".
        /// This value should be parsable to a type of <c>ulong</c> (User ID).
        /// </param>
        /// <returns>A <c>Task</c> object, which we can await until this method completes successfully.</returns>

        public async Task RemoveMutedRole(Dictionary<string, string> Parameters) {
            ulong UserID = ulong.Parse(Parameters["UserID"]);

            IGuild Guild = DiscordSocketClient.GetGuild(BotConfiguration.GuildID);

            IRole Role = Guild.GetRole(ModerationConfiguration.MutedRoleID);

            IGuildUser User = await Guild.GetUserAsync(UserID);

            if (User != null)
                await User.RemoveRoleAsync(Role);
        }
    }

}
