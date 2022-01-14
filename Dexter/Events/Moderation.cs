using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Databases.FinalWarns;
using Dexter.Databases.Infractions;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
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
		/// The LanguageConfiguration utilized to obtain certain information such as time zone management data.
		/// </summary>

		public LanguageConfiguration LanguageConfiguration { get; set; }

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

			using var infractionsDB = scope.ServiceProvider.GetRequiredService<InfractionsDB>();

			await infractionsDB.DexterProfiles.AsQueryable().ForEachAsync(
				async dexProfile =>
				{
					if (dexProfile.InfractionAmount < ModerationConfiguration.MaxPoints)
						if (!TimerService.TimerExists(dexProfile.CurrentPointTimer))
							dexProfile.CurrentPointTimer = await CreateEventTimer(
								IncrementPoints,
								new() { { "UserID", dexProfile.UserID.ToString() } },
								ModerationConfiguration.SecondsTillPointIncrement,
								TimerType.Expire
							);
				}
			);

			await infractionsDB.EnsureSaved();
		}

		/// <summary>
		/// The ReactionRemovedLog records if a reaction has been quickly removed from a message, as is important to find if someone has been spamming reactions.
		/// </summary>
		/// <param name="userMessage">An instance of the message the reaction has been removed from.</param>
		/// <param name="msgChannel">The channel of which the reaction has been removed in - used to check if it's from a channel that is often removed from.</param>
		/// <param name="reaction">An object containing the reaction that had been removed.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public async Task ReactionRemovedLog(Cacheable<IUserMessage, ulong> userMessage, Cacheable<IMessageChannel, ulong> msgChannel, SocketReaction reaction)
		{
			if (ModerationConfiguration.DisabledReactionChannels.Contains(msgChannel.Id))
				return;

			IMessage cachedMsg = await userMessage.GetOrDownloadAsync();

			if (cachedMsg == null)
				return;

			if (string.IsNullOrEmpty(cachedMsg.Content))
				return;

			await BuildEmbed(EmojiEnum.Unknown)
				.WithAuthor(reaction.User.Value)
				.WithDescription($"**Reaction removed in <#{msgChannel.Id}> by {reaction.User.GetValueOrDefault().GetUserInformation()}**")
				.AddField("Message", cachedMsg.Content.Length > 50 ? string.Concat(cachedMsg.Content.AsSpan(0, 50), "...") : cachedMsg.Content)
				.AddField("Reaction Removed", reaction.Emote)
				.WithFooter($"Author: {cachedMsg.Author.Id} | Message ID: {cachedMsg.Id}")
				.SendEmbed(await CreateOrGetWebhook(ModerationConfiguration.WebhookChannel, ModerationConfiguration.WebhookName));
		}

		/// <summary>
		/// Specifies a set of constraints that the general behaviour of infractions can be altered by. 
		/// </summary>

		public class InfractionOptions {
			/// <summary>
			/// Whether to DM the user about this infraction on issue.
			/// </summary>
			public bool dmUser = true;

			/// <summary>
			/// Whether to deduct points from the user upon issuing the infraction.
			/// </summary>
			public bool deductPoints = true;

            /// <summary>
            /// If <see langword="default"/>, ignore this option. Otherwise, set the mute duration to match this mute end time.
            /// </summary>
            public DateTimeOffset muteuntil = default;

			/// <summary>
			/// If <see langword="default"/>, ignore this option. Otherwise, represents an override for the logged date of the infraction. 
			/// </summary>
			public DateTimeOffset date = default;

			/// <summary>
			/// If <see langword="default"/>, ignore this option. The logged duration of the mute.
			/// </summary>
			public TimeSpan logduration = default;

			/// <summary>
			/// A visual description of the formats allowed as options
			/// </summary>

			public const string OPTION_FORMATS = "`nodm`, `nodeduction`, `date:DATE`, `muteuntil:DATE`, `logduration:(DDd)(HHh)(MMm)(SSs)` (d, h, m, and s are literal, () indicates optional elements)";
			/// <summary>
			/// Attempts to parse a set of infraction options from raw text input.
			/// </summary>
			/// <param name="arguments">The text input to parse the text options from.</param>
			/// <param name="languageConfiguration">The language configuration options used to parse certain details such as timezones.</param>
			/// <returns>An <see cref="InfractionOptions"/> object stemming from the given <paramref name="arguments"/>.</returns>
			/// <exception cref="ArgumentException"></exception>
			public static InfractionOptions Parse(string arguments, LanguageConfiguration languageConfiguration)
            {
				InfractionOptions opts = new();

				foreach(string s in arguments.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
					switch(s.ToLower())
                    {
						case "nodm":
						case "no-dm":
						case "no_dm":
						case "no dm":
							opts.dmUser = false;
							break;
						case "nodeduction":
						case "no-deduction":
						case "no_deduction":
						case "no deduction":
							opts.deductPoints = false;
							break;
						default:
							Match m;
							m = Regex.Match(s, @"mute[_\-\s]?until\s?[=:]?([a-z0-9\/:.+\-\s]+)", RegexOptions.IgnoreCase);
							if (m.Success)
                            {
								string value = m.Groups[1].Value.Trim();
								if (DateTimeOffset.TryParse(m.Groups[1].Value, out opts.muteuntil)) break;
								if (LanguageHelper.TryParseTime(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, languageConfiguration, out opts.muteuntil, out string error)) break;
								throw new ArgumentException("Unable to parse mute-until argument. " + error);
                            }
							m = Regex.Match(s, @"date\s?[=:]?([a-z0-9\/:.+\-\s]+)", RegexOptions.IgnoreCase);
							if (m.Success)
							{
								string value = m.Groups[1].Value.Trim();
								if (DateTimeOffset.TryParse(m.Groups[1].Value, out opts.date)) break;
								if (LanguageHelper.TryParseTime(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, languageConfiguration, out opts.date, out string error)) break;
								throw new ArgumentException("Unable to parse date argument. " + error);
							}
							m = Regex.Match(s, @"log[_\-\s]?duration\s?[=:]?([a-z0-9:.\s]+)", RegexOptions.IgnoreCase);
							if (m.Success)
							{
								string value = m.Groups[1].Value.Trim();
								if (TimeSpan.TryParse(m.Groups[1].Value, out opts.logduration)) break;
								if (LanguageHelper.TryParseSpan(m.Groups[1].Value, out opts.logduration, out string error)) break;
								throw new ArgumentException("Unable to parse log-duration argument. " + error);
							}
							throw new ArgumentException($"Unrecognized option: `{s}`; it must follow one of the following formats: [{OPTION_FORMATS}]");
                    }
                }

				return opts;
            }
		}

		/// <summary>
		/// Mutes a user for a specific amount of time and removes a specified number of points from their Dexter Profile.
		/// This mute is recorded in the corresponding database.
		/// </summary>
		/// <param name="pointsLogged">The amount of points to remove from the user's Dexter Profile for automoderation purposes.</param>
		/// <param name="user">The target user.</param>
		/// <param name="time">The duration of the mute.</param>
		/// <param name="reason">A string description of the reason why the mute was issued.</param>
		/// <param name="context">If accessed outside this module, context is needed..</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public async Task IssueInfraction(short pointsLogged, IGuildUser user, TimeSpan time, string reason, SocketCommandContext context)
		{
			using var scope = ServiceProvider.CreateScope();

			using var infractionsDB = scope.ServiceProvider.GetRequiredService<InfractionsDB>();

			using var finalWarnsDB = scope.ServiceProvider.GetRequiredService<FinalWarnsDB>();

			InfractionOptions options = new();
			Match m = Regex.Match(reason, @"^opt(ion)?s?\s?[=:]?\s?{(?<arg>[^}]+)}", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
			if (m.Success)
            {
				string arg = m.Groups[1].Value.Trim();
				try
                {
					options = InfractionOptions.Parse(arg, LanguageConfiguration);
                }
				catch (Exception e)
                {
					await context.Channel.SendMessageAsync(e.Message);
					return;
                }
				reason = reason[(m.Groups[1].Index + m.Groups[1].Length + 1)..].Trim();
            }

			if (reason.Length > 750 + (options.dmUser ? 0 : 100)) //Extra length in case extra tags added to the reason cause this excedence of characters.
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Reason too long.")
					.WithDescription($"Your reason should be at most 750 characters long. \nWe found {reason.Length} instead.")
					.SendEmbed(context.Channel);
				return;
			}

			// Processing of options:

			short toDeduct = options.deductPoints ? pointsLogged : (short)0;

			// Dexter Notifications if the user has been in breach of a parameter.

			DexterProfile dexProfile = infractionsDB.GetOrCreateProfile(user.Id);

			string notification = string.Empty;

			if (dexProfile.InfractionAmount > ModerationConfiguration.InfractionNotification &&
					dexProfile.InfractionAmount - toDeduct <= ModerationConfiguration.InfractionNotification)
				notification += $"It seems as though the user {user.GetUserInformation()} is currently standing on {dexProfile.InfractionAmount - toDeduct} points.\n";

			foreach (Dictionary<string, int> notifications in ModerationConfiguration.InfractionNotifications)
			{
				int pts = 0;

				infractionsDB.GetInfractions(user.Id)
					.Where(infraction => infraction.TimeOfIssue > DateTimeOffset.UtcNow.ToUnixTimeSeconds() - notifications["Days"] * 86400)
					.Select(infraction => infraction.PointCost).ToList().ForEach(p => pts += p);

				if (dexProfile.InfractionAmount < notifications["Points"] && dexProfile.InfractionAmount + toDeduct >= notifications["Points"])
					notification += $"It seems as though the user {user.GetUserInformation()} has had {pts + toDeduct} points deducted in {notifications["Days"]} days.\n";
			}

			if (notification.Length > 0)
			{
				await (DiscordShardedClient.GetChannel(BotConfiguration.ModerationLogChannelID) as ITextChannel)
					.SendMessageAsync($"**Frequently Warned User >>>** <@&{BotConfiguration.AdministratorRoleID}>",
						embed: BuildEmbed(EmojiEnum.Wut)
						.WithTitle($"Frequent Rule Breaker Inbound!")
						.WithDescription($"Haiya!\n{notification}Perhaps this is something the <@&{BotConfiguration.AdministratorRoleID}> can dwell on. <3")
						.Build()
				);
			}

			if (IsUserFinalWarned(user) && toDeduct >= ModerationConfiguration.FinalWarnNotificationThreshold)
			{
				await (DiscordShardedClient.GetChannel(BotConfiguration.ModerationLogChannelID) as ITextChannel)
					.SendMessageAsync($"**Final Warned User Infraction >>>** <@&{BotConfiguration.AdministratorRoleID}> <@{context.User.Id}>",
						embed: BuildEmbed(EmojiEnum.Wut)
						.WithTitle($"Final Warned User has been {(time.TotalSeconds > 0 ? "muted" : "warned")}!")
						.WithDescription($"Haiya! User <@{user.Id}> has been {(time.TotalSeconds > 0 ? "muted" : "warned")} for `{reason}` and has lost {pointsLogged} point{(pointsLogged != 1 ? "s" : "")}. They currently have {dexProfile.InfractionAmount - toDeduct} point{(dexProfile.InfractionAmount - toDeduct != 1 ? "s" : "")}.")
						.Build()
				);
			}

			// Apply point deductions and possible mutes.

			dexProfile.InfractionAmount -= toDeduct;

			if (pointsLogged == 0)
			{
				await MuteUser(user, time);
			}
			else
			{
				if (options.muteuntil != default)
				{
					time = options.muteuntil.Subtract(DateTimeOffset.Now);
				}
				else
				{
					TimeSpan? additionalTime = null;

					if (dexProfile.InfractionAmount == 0)
						additionalTime = TimeSpan.FromMinutes(30);
					else if (dexProfile.InfractionAmount == -1)
						additionalTime = TimeSpan.FromMinutes(45);
					else if (dexProfile.InfractionAmount == -2)
						additionalTime = TimeSpan.FromHours(1);
					else if (dexProfile.InfractionAmount == -3)
						additionalTime = TimeSpan.FromHours(2);
					else if (dexProfile.InfractionAmount <= -4)
						additionalTime = TimeSpan.FromHours(3);

					if (additionalTime.HasValue)
					{
						time = time.Add(additionalTime.Value);

						reason += $"\n**Automatic mute of {additionalTime.Value.Humanize(2)} applied in addition by {DiscordShardedClient.CurrentUser.Username} for frequent warnings and/or rulebreaks.**";
					}
				}

				if (time.TotalSeconds > 0)
					await MuteUser(user, time);
			}

			if (options.logduration == default && time > TimeSpan.Zero) options.logduration = time;
			if (!TimerService.TimerExists(dexProfile.CurrentPointTimer))
				dexProfile.CurrentPointTimer = await CreateEventTimer(IncrementPoints, new() { { "UserID", user.Id.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);

			// Add the infraction to the database.

			int infractionID = infractionsDB.Infractions.Any() ? infractionsDB.Infractions.Max(w => w.InfractionID) + 1 : 1;

			int totalInfractions = infractionsDB.GetInfractions(user.Id).Length + 1;

			infractionsDB.Infractions.Add(new Infraction()
			{
				Issuer = context.User.Id,
				Reason = reason,
				User = user.Id,
				InfractionID = infractionID,
				EntryType = EntryType.Issue,
				TimeOfIssue = options.date == default ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : options.date.ToUnixTimeSeconds(),
				PointCost = pointsLogged,
				InfractionTime = options.logduration == default ? Convert.ToInt32(time.TotalSeconds) : Convert.ToInt32(options.logduration.TotalSeconds)
			});

			InfractionType infractionType = pointsLogged == 0 && time.TotalSeconds == 0 ? InfractionType.IndefiniteMute : time.TotalSeconds > 0 ? InfractionType.Mute : InfractionType.Warning;

			// Send the embed of the infraction to the moderation channel.
			EmbedBuilder eb = context.Channel.Id == ModerationConfiguration.StaffBotsChannel ?

				// If we are in the staff bots channel, send the embed with all the user's info aswell.
				BuildEmbed(EmojiEnum.Love)
				   .WithTitle($"{Regex.Replace(infractionType.ToString(), "([A-Z])([a-z]*)", " $1$2")} #{infractionID} Issued! Current Points: {dexProfile.InfractionAmount}.")
				   .WithDescription($"{(infractionType == InfractionType.Warning ? "Warned" : "Muted")} {user.GetUserInformation()}" +
					   $"{(infractionType == InfractionType.Mute ? $" for **{time.Humanize(2)}**" : "")} who currently has **{totalInfractions} " +
					   $"{(totalInfractions == 1 ? "infraction" : "infractions")}** and has had **{pointsLogged} {(pointsLogged == 1 ? "point" : "points")} deducted.**")
				   .AddField("Issued By", context.User.GetUserInformation())
				   .AddField(time.TotalSeconds > 0, "Total Mute Time", $"{(options.logduration != default ? options.logduration : time).Humanize(2)}.")
				   .AddField("Reason", reason) :

				// If we are in a public channel we don't want the user's warnings public.
				BuildEmbed(EmojiEnum.Love)
					.WithTitle($"{(infractionType == InfractionType.Warning ? "Warned" : "Muted")} {user.Username}#{user.Discriminator}")
					.WithDescription($"<@{user.Id}> has been {(infractionType == InfractionType.Warning ? "warned" : "muted")} by <@{context.User.Id}> {(infractionType == InfractionType.Mute ? $" for **{time.Humanize(2)}**" : "")} due to `{reason}`");

			// Send the embed into the channel.
			if (options.dmUser)
				await eb.SendDMAttachedEmbed(context.Channel, BotConfiguration, user,

				// Send the warning notification to the user.
				BuildEmbed(EmojiEnum.Love)
					.WithTitle($"You were issued a {(infractionType == InfractionType.Warning ? "warning" : $"mute{(infractionType == InfractionType.Mute ? $" of {time.Humanize(2)}" : "")}")} from {context.Guild.Name}.")
					.WithDescription(
						pointsLogged == 0 ? "Please hold on! A moderator will likely be right with you-" :

						$"You have had a total of {totalInfractions} {(totalInfractions == 1 ? "infraction" : "infractions")} and are on {dexProfile.InfractionAmount} {(dexProfile.InfractionAmount == 1 ? "point" : "points")}, you regain one point every 24 hours. " +
						(infractionType == InfractionType.Warning ?
						"Please note that whenever we warn you verbally, you will receive a logged warning. This is not indicative of a mute or ban." :
						$"Please read over the <#{ModerationConfiguration.RulesAndInfoChannel}> channel if you have not already done so, even if it's just for a refresher, as to make sure your behaviour meets the standards of the server. <3")
					)
					.AddField("Reason", reason));
			else
				await eb.SendEmbed(context.Channel);

			try
			{
				await infractionsDB.EnsureSaved();
			}
			catch (Exception e)
            {
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("We were unable to save the infraction.")
					.WithDescription(e.Message + "\n You may try again by using the command provided in the retry command field below.")
					.AddField("Retry Command:", 
					 $"{BotConfiguration.Prefix}{(infractionType == InfractionType.Warning ? "warn" : "mute")} {pointsLogged} {user.Id}" +
					 $"{(time > TimeSpan.Zero ? $" {time.Days}d{time.Hours}h{time.Minutes}m{time.Seconds}s " : " ")}" +
					 $"options{{nodm,date={(options.date != default ? options.date : DateTime.Now):yyyy/MM/ddTHH:mm:ss}" +
						$"{(time > TimeSpan.Zero ? $",muteuntil={DateTime.Now.Add(time):yyyy/MM/ddTHH:mm:ss}" : "")}" +
						$"{(options.logduration != time && options.logduration > TimeSpan.Zero ? $",logduration={options.logduration.Days}d{options.logduration.Hours}h{options.logduration.Minutes}m{options.logduration.Seconds}s" : "")}" +
						$"{(pointsLogged != toDeduct ? ",nodeduction" : "")}" +
						$"}} {reason}")
					.SendEmbed(context.Channel);
            }
		}

		/// <summary>
		/// Manually adds points to a user's Dexter Profile.
		/// </summary>
		/// <param name="parameters">
		/// A string-string dictionary containing a definition for "UserID".
		/// This value should be parsable to a type of <c>ulong</c> (User ID).
		/// </param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public async Task IncrementPoints(Dictionary<string, string> parameters)
		{
			using var scope = ServiceProvider.CreateScope();

			using var infractionDB = scope.ServiceProvider.GetRequiredService<InfractionsDB>();

			ulong userID = ulong.Parse(parameters["UserID"]);

			DexterProfile dexProfile = infractionDB.GetOrCreateProfile(userID);

			if (dexProfile.InfractionAmount < ModerationConfiguration.MaxPoints)
			{
				dexProfile.InfractionAmount++;
				if (dexProfile.InfractionAmount < ModerationConfiguration.MaxPoints)
					dexProfile.CurrentPointTimer = await CreateEventTimer(IncrementPoints, new() { { "UserID", userID.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);
				else
					dexProfile.CurrentPointTimer = string.Empty;
			}
			else
			{
				if (dexProfile.InfractionAmount > ModerationConfiguration.MaxPoints)
					dexProfile.InfractionAmount = ModerationConfiguration.MaxPoints;

				dexProfile.CurrentPointTimer = string.Empty;
			}

			await infractionDB.EnsureSaved();
		}

		/// <summary>
		/// Checks whether an active final warn is logged for <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The user to query in the database.</param>
		/// <returns><see langword="true"/> if the user has an active final warn; <see langword="false"/> otherwise.</returns>

		public bool IsUserFinalWarned(IGuildUser user)
		{
			using var scope = ServiceProvider.CreateScope();

			using var finalWarnsDB = scope.ServiceProvider.GetRequiredService<FinalWarnsDB>();

			FinalWarn warn = finalWarnsDB.FinalWarns.Find(user.Id);

			return warn != null && warn.EntryType == EntryType.Issue;
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
				await TimerService.RemoveTimer(DexterProfile.CurrentMute);

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

			await InfractionsDB.EnsureSaved();
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

			await InfractionsDB.EnsureSaved();
		}

	}

}
