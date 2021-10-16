using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Commands;
using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Databases.FinalWarns;
using Dexter.Databases.Infractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dexter.Events
{

    /// <summary>
    /// The Moderation Service deals with logging certain events to a channel.
    /// Currently, this only includes the logging of reactions to the channel.
    /// </summary>

    public class Moderation : Event
    {

        /// <summary>
        /// The ModerationService is used to find and create the moderation logs webhook.
        /// </summary>

        public ModerationConfiguration ModerationConfiguration { get; set; }

        /// <summary>
        /// The ModeratorCommands references Dexter's moderation module and grants access to its commands.
        /// </summary>

        public ModeratorCommands ModeratorCommands { get; set; }

        /// <summary>
        /// The DiscordWebhookClient is used for sending messages to the logging channel.
        /// </summary>

        public DiscordWebhookClient DiscordWebhookClient;

        /// <summary>
        /// The Initialize method adds the ReactionRemoved hook to the ReactionRemovedLog method.
        /// It also hooks the ready event to the CreateWebhook delegate.
        /// </summary>

        public override void InitializeEvents()
        {
            DiscordShardedClient.ReactionRemoved += ReactionRemovedLog;
            DiscordShardedClient.ShardReady += (DiscordSocketClient _) => DexterProfileChecks();
        }

        /// <summary>
        /// <para>Checks that all users who do not have the maximum amount of points in their Dexter Profile have an EventTimer that increments one point in their Dexter Profile.</para>
        /// <para>This serves as a way to allow slow-paced regeneration of Dexter Profile points.</para>
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task DexterProfileChecks()
        {
            using var scope = ServiceProvider.CreateScope();

            using var InfractionsDB = scope.ServiceProvider.GetRequiredService<InfractionsDB>();

            await InfractionsDB.DexterProfiles.AsQueryable().ForEachAsync(
                async DexterProfile =>
                {
                    if (DexterProfile.InfractionAmount < ModerationConfiguration.MaxPoints)
                        if (!TimerService.TimerExists(DexterProfile.CurrentPointTimer))
                            DexterProfile.CurrentPointTimer = await CreateEventTimer(
                                IncrementPoints,
                                new() { { "UserID", DexterProfile.UserID.ToString() } },
                                ModerationConfiguration.SecondsTillPointIncrement,
                                TimerType.Expire
                            );
                }
            );

            await InfractionsDB.SaveChangesAsync();
        }

        /// <summary>
        /// The ReactionRemovedLog records if a reaction has been quickly removed from a message, as is important to find if someone has been spamming reactions.
        /// </summary>
        /// <param name="UserMessage">An instance of the message the reaction has been removed from.</param>
        /// <param name="MessageChannel">The channel of which the reaction has been removed in - used to check if it's from a channel that is often removed from.</param>
        /// <param name="Reaction">An object containing the reaction that had been removed.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task ReactionRemovedLog(Cacheable<IUserMessage, ulong> UserMessage, Cacheable<IMessageChannel, ulong> MessageChannel, SocketReaction Reaction)
        {
            if (ModerationConfiguration.DisabledReactionChannels.Contains(MessageChannel.Id))
                return;

            IMessage CachedMessage = await UserMessage.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (string.IsNullOrEmpty(CachedMessage.Content))
                return;

            await BuildEmbed(EmojiEnum.Unknown)
                .WithAuthor(Reaction.User.Value)
                .WithDescription($"**Reaction removed in <#{MessageChannel.Id}> by {Reaction.User.GetValueOrDefault().GetUserInformation()}**")
                .AddField("Message", CachedMessage.Content.Length > 50 ? string.Concat(CachedMessage.Content.AsSpan(0, 50), "...") : CachedMessage.Content)
                .AddField("Reaction Removed", Reaction.Emote)
                .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                .SendEmbed(await CreateOrGetWebhook(ModerationConfiguration.WebhookChannel, ModerationConfiguration.WebhookName));
        }

        /// <summary>
        /// Mutes a user for a specific amount of time and removes a specified number of points from their Dexter Profile.
        /// This mute is recorded in the corresponding database.
        /// </summary>
        /// <param name="PointsDeducted">The amount of points to remove from the user's Dexter Profile for automoderation purposes.</param>
        /// <param name="User">The target user.</param>
        /// <param name="Time">The duration of the mute.</param>
        /// <param name="Reason">A string description of the reason why the mute was issued.</param>
        /// <param name="Context">If accessed outside this module, context is needed..</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task IssueInfraction(short PointsDeducted, IGuildUser User, TimeSpan Time, string Reason, SocketCommandContext Context)
        {
            using var scope = ServiceProvider.CreateScope();

            using var InfractionsDB = scope.ServiceProvider.GetRequiredService<InfractionsDB>();

            using var FinalWarnsDB = scope.ServiceProvider.GetRequiredService<FinalWarnsDB>();

            if (Reason.Length > 750)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Reason too long.")
                    .WithDescription($"Your reason should be at most 750 characters long. \nWe found {Reason.Length} instead.")
                    .SendEmbed(Context.Channel);
                return;
            }

            // Dexter Notifications if the user has been in breach of a parameter.

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User.Id);

            string Notification = string.Empty;

            if (DexterProfile.InfractionAmount > ModerationConfiguration.InfractionNotification &&
                    DexterProfile.InfractionAmount - PointsDeducted <= ModerationConfiguration.InfractionNotification)
                Notification += $"It seems as though the user {User.GetUserInformation()} is currently standing on {DexterProfile.InfractionAmount - PointsDeducted} points.\n";

            foreach (Dictionary<string, int> Notifications in ModerationConfiguration.InfractionNotifications)
            {
                int Points = 0;

                InfractionsDB.GetInfractions(User.Id)
                    .Where(Infraction => Infraction.TimeOfIssue > DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Notifications["Days"] * 86400)
                    .Select(Infraction => Infraction.PointCost).ToList().ForEach(Point => Points += Point);

                if (DexterProfile.InfractionAmount < Notifications["Points"] && DexterProfile.InfractionAmount + PointsDeducted >= Notifications["Points"])
                    Notification += $"It seems as though the user {User.GetUserInformation()} has had {Points + PointsDeducted} points deducted in {Notifications["Days"]} days.\n";
            }

            if (Notification.Length > 0)
            {
                await (DiscordShardedClient.GetChannel(BotConfiguration.ModerationLogChannelID) as ITextChannel)
                    .SendMessageAsync($"**Frequently Warned User >>>** <@&{BotConfiguration.AdministratorRoleID}>",
                        embed: BuildEmbed(EmojiEnum.Wut)
                        .WithTitle($"Frequent Rule Breaker Inbound!")
                        .WithDescription($"Haiya!\n{Notification}Perhaps this is something the <@&{BotConfiguration.AdministratorRoleID}> can dwell on. <3")
                        .Build()
                );
            }

            if (FinalWarnsDB.IsUserFinalWarned(User) && PointsDeducted >= ModerationConfiguration.FinalWarnNotificationThreshold)
            {
                await (DiscordShardedClient.GetChannel(BotConfiguration.ModerationLogChannelID) as ITextChannel)
                    .SendMessageAsync($"**Final Warned User Infraction >>>** <@&{BotConfiguration.AdministratorRoleID}> <@{Context.User.Id}>",
                        embed: BuildEmbed(EmojiEnum.Wut)
                        .WithTitle($"Final Warned User has been {(Time.TotalSeconds > 0 ? "muted" : "warned")}!")
                        .WithDescription($"Haiya! User <@{User.Id}> has been {(Time.TotalSeconds > 0 ? "muted" : "warned")} for `{Reason}` and has lost {PointsDeducted} point{(PointsDeducted != 1 ? "s" : "")}. They currently have {DexterProfile.InfractionAmount - PointsDeducted} point{(DexterProfile.InfractionAmount - PointsDeducted != 1 ? "s" : "")}.")
                        .Build()
                );
            }

            // Apply point deductions and possible mutes.

            DexterProfile.InfractionAmount -= PointsDeducted;

            if (PointsDeducted == 0)
            {
                await MuteUser(User, Time);
            }
            else
            {
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

                if (AdditionalTime.HasValue)
                {
                    Time = Time.Add(AdditionalTime.Value);

                    Reason += $"\n**Automatic mute of {AdditionalTime.Value.Humanize(2)} applied in addition by {DiscordShardedClient.CurrentUser.Username} for frequent warnings and/or rulebreaks.**";
                }

                if (Time.TotalSeconds > 0)
                    await MuteUser(User, Time);
            }

            if (!TimerService.TimerExists(DexterProfile.CurrentPointTimer))
                DexterProfile.CurrentPointTimer = await CreateEventTimer(IncrementPoints, new() { { "UserID", User.Id.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);

            // Add the infraction to the database.

            int InfractionID = InfractionsDB.Infractions.Any() ? InfractionsDB.Infractions.Max(Warning => Warning.InfractionID) + 1 : 1;

            int TotalInfractions = InfractionsDB.GetInfractions(User.Id).Length + 1;

            InfractionsDB.Infractions.Add(new Infraction()
            {
                Issuer = Context.User.Id,
                Reason = Reason,
                User = User.Id,
                InfractionID = InfractionID,
                EntryType = EntryType.Issue,
                TimeOfIssue = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                PointCost = PointsDeducted,
                InfractionTime = Convert.ToInt32(Time.TotalSeconds)
            });

            InfractionType InfractionType = PointsDeducted == 0 && Time.TotalSeconds == 0 ? InfractionType.IndefiniteMute : Time.TotalSeconds > 0 ? InfractionType.Mute : InfractionType.Warning;

            // Send the embed of the infraction to the moderation channel.
            await (Context.Channel.Id == ModerationConfiguration.StaffBotsChannel ?

                // If we are in the staff bots channel, send the embed with all the user's info aswell.
                BuildEmbed(EmojiEnum.Love)
                   .WithTitle($"{Regex.Replace(InfractionType.ToString(), "([A-Z])([a-z]*)", " $1$2")} #{InfractionID} Issued! Current Points: {DexterProfile.InfractionAmount}.")
                   .WithDescription($"{(InfractionType == InfractionType.Warning ? "Warned" : "Muted")} {User.GetUserInformation()}" +
                       $"{(InfractionType == InfractionType.Mute ? $" for **{Time.Humanize(2)}**" : "")} who currently has **{TotalInfractions} " +
                       $"{(TotalInfractions == 1 ? "infraction" : "infractions")}** and has had **{PointsDeducted} {(PointsDeducted == 1 ? "point" : "points")} deducted.**")
                   .AddField("Issued By", Context.User.GetUserInformation())
                   .AddField(Time.TotalSeconds > 0, "Total Mute Time", $"{Time.Humanize(2)}.")
                   .AddField("Reason", Reason) :

                // If we are in a public channel we don't want the user's warnings public.
                BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"{(InfractionType == InfractionType.Warning ? "Warned" : "Muted")} {User.Username}#{User.Discriminator}")
                    .WithDescription($"<@{User.Id}> has been {(InfractionType == InfractionType.Warning ? "warned" : "muted")} by <@{Context.User.Id}> {(InfractionType == InfractionType.Mute ? $" for **{Time.Humanize(2)}**" : "")} due to `{Reason}`"))

                // Send the embed into the channel.
                .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User,

                    // Send the warning notification to the user.
                    BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"You were issued a {(InfractionType == InfractionType.Warning ? "warning" : $"mute{(InfractionType == InfractionType.Mute ? $" of {Time.Humanize(2)}" : "")}")} from {Context.Guild.Name}.")
                        .WithDescription(
                            PointsDeducted == 0 ? "Please hold on! A moderator will likely be right with you-" :

                            $"You have had a total of {TotalInfractions} {(TotalInfractions == 1 ? "infraction" : "infractions")} and are on {DexterProfile.InfractionAmount} {(DexterProfile.InfractionAmount == 1 ? "point" : "points")}, you regain one point every 24 hours. " +
                            (InfractionType == InfractionType.Warning ?
                            "Please note that whenever we warn you verbally, you will receive a logged warning. This is not indicative of a mute or ban." :
                            $"Please read over the <#{ModerationConfiguration.RulesAndInfoChannel}> channel if you have not already done so, even if it's just for a refresher, as to make sure your behaviour meets the standards of the server. <3")
                        )
                        .AddField("Reason", Reason)

                );

            await InfractionsDB.SaveChangesAsync();
        }

        /// <summary>
        /// Manually adds points to a user's Dexter Profile.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "UserID".
        /// This value should be parsable to a type of <c>ulong</c> (User ID).
        /// </param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task IncrementPoints(Dictionary<string, string> Parameters)
        {
            using var scope = ServiceProvider.CreateScope();

            using var InfractionsDB = scope.ServiceProvider.GetRequiredService<InfractionsDB>();

            ulong UserID = ulong.Parse(Parameters["UserID"]);

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(UserID);

            if (DexterProfile.InfractionAmount < ModerationConfiguration.MaxPoints)
            {
                DexterProfile.InfractionAmount++;
                if (DexterProfile.InfractionAmount < ModerationConfiguration.MaxPoints)
                    DexterProfile.CurrentPointTimer = await CreateEventTimer(IncrementPoints, new() { { "UserID", UserID.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);
                else
                    DexterProfile.CurrentPointTimer = string.Empty;
            }
            else
            {
                if (DexterProfile.InfractionAmount > ModerationConfiguration.MaxPoints)
                    DexterProfile.InfractionAmount = ModerationConfiguration.MaxPoints;

                DexterProfile.CurrentPointTimer = string.Empty;
            }

            await InfractionsDB.SaveChangesAsync();
        }

        /// <summary>
        /// Issues a mute to a target <paramref name="User"/> for a duration of <paramref name="Time"/>; but doesn't save it to the user's records.
        /// </summary>
        /// <param name="User">The user to be muted.</param>
        /// <param name="Time">The duration of the mute.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method finishes successfully.</returns>

        public async Task MuteUser(IGuildUser User, TimeSpan Time)
        {
            using var scope = ServiceProvider.CreateScope();

            using var InfractionsDB = scope.ServiceProvider.GetRequiredService<InfractionsDB>();

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User.Id);

            if (TimerService.TimerExists(DexterProfile.CurrentMute))
                TimerService.RemoveTimer(DexterProfile.CurrentMute);

            try
            {
                foreach (ulong MutedRole in ModerationConfiguration.MutedRoles)
                {
                    IRole Muted = User.Guild.GetRole(MutedRole);

                    if (!User.RoleIds.Contains(MutedRole))
                        await User.AddRoleAsync(Muted);
                }
            }
            catch (Discord.Net.HttpException Error)
            {
                await (DiscordShardedClient.GetChannel(BotConfiguration.ModerationLogChannelID) as ITextChannel)
                    .SendMessageAsync($"**Missing Role Management Permissions >>>** <@&{BotConfiguration.AdministratorRoleID}>",
                        embed: BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Error!")
                        .WithDescription($"Couldn't mute user <@{User.Id}> ({User.Id}) for {Time.Humanize()}.")
                        .AddField("Error:", Error.Message)
                        .Build()
                );
            }

            if (Time == TimeSpan.FromSeconds(0))
                return;

            DexterProfile.CurrentMute = await CreateEventTimer(RemoveMutedRole, new() { { "UserID", User.Id.ToString() } }, Convert.ToInt32(Time.TotalSeconds), TimerType.Expire);

            await InfractionsDB.SaveChangesAsync();
        }

        /// <summary>
        /// Manually removes points from a user's Dexter Profile.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "UserID".
        /// This value should be parsable to a type of <c>ulong</c> (User ID).
        /// </param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task RemoveMutedRole(Dictionary<string, string> Parameters)
        {
            using var scope = ServiceProvider.CreateScope();

            using var InfractionsDB = scope.ServiceProvider.GetRequiredService<InfractionsDB>();

            ulong UserID = ulong.Parse(Parameters["UserID"]);

            IGuild Guild = DiscordShardedClient.GetGuild(BotConfiguration.GuildID);

            IGuildUser User = await Guild.GetUserAsync(UserID);

            foreach (ulong MutedRole in ModerationConfiguration.MutedRoles)
            {
                IRole Muted = User.Guild.GetRole(MutedRole);

                if (User.RoleIds.Contains(MutedRole))
                    await User.RemoveRoleAsync(Muted);
            }

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(UserID);

            DexterProfile.CurrentMute = string.Empty;

            await InfractionsDB.SaveChangesAsync();
        }

    }

}
