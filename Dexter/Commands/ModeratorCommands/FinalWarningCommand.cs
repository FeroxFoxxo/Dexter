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
		/// Issues a final warning to a target <paramref name="user"/>, mutes then for <paramref name="muteDuration"/>, and adds a detailed entry about the final warn to the Final Warns database.
		/// </summary>
		/// <param name="user">The target user to final warn.</param>
		/// <param name="muteDuration">The duration of the mute attached to the final warn.</param>
		/// <param name="reason">The reason behind the final warn.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("finalwarn")]
		[Summary("Issues a final warning to a user, mutes them and records the final warn.")]
		[Alias("warnfinal")]
		[RequireModerator]
		[BotChannel]

		public async Task IssueFinalWarn(IGuildUser user, TimeSpan muteDuration, [Remainder] string reason)
		{
			short pointsDeducted = ModerationConfiguration.FinalWarningPointsDeducted;

			if (ModerationService.IsUserFinalWarned(user))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("User is already final warned!")
					.WithDescription($"The target user, {user.GetUserInformation()}, already has an active final warn. If you wish to overwrite this final warn, first remove the already existing one.")
					.SendEmbed(Context.Channel);

				return;
			}

			DexterProfile dexProfile = InfractionsDB.GetOrCreateProfile(user.Id);

			dexProfile.InfractionAmount -= pointsDeducted;

			if (!TimerService.TimerExists(dexProfile.CurrentPointTimer))
				dexProfile.CurrentPointTimer = await CreateEventTimer(ModerationService.IncrementPoints, new() { { "UserID", user.Id.ToString() } }, ModerationConfiguration.SecondsTillPointIncrement, TimerType.Expire);


			ulong warningLogID = 0;

			if (ModerationConfiguration.FinalWarningsManageRecords)
			{
				warningLogID = (await (DiscordShardedClient.GetChannel(ModerationConfiguration.FinalWarningsChannelID) as ITextChannel).SendMessageAsync(
					$"**Final Warning Issued >>>** <@&{BotConfiguration.ModeratorRoleID}>\n" +
					$"**User**: {user.GetUserInformation()}\n" +
					$"**Issued on**: {DateTime.Now:MM/dd/yyyy}\n" +
					$"**Reason**: {reason}")).Id;
			}

			SetOrCreateFinalWarn(pointsDeducted, Context.User as IGuildUser, user, muteDuration, reason, warningLogID);

			await ModerationService.MuteUser(user, muteDuration);

			try
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"ðŸš¨ You were issued a **FINAL WARNING** from {Context.Guild.Name}! ðŸš¨")
					.WithDescription(reason)
					.AddField("Points Deducted:", pointsDeducted, true)
					.AddField("Mute Duration:", muteDuration.Humanize(), true)

					.SendEmbed(await user.CreateDMChannelAsync());

				await BuildEmbed(EmojiEnum.Love)
					.WithTitle("Message sent successfully!")
					.WithDescription($"The target user, {user.GetUserInformation()}, has been informed of their current status.")
					.AddField("Mute Duration:", muteDuration.Humanize(), true)
					.AddField("Points Deducted:", pointsDeducted, true)
					.AddField("Issued By:", Context.User.GetUserInformation())
					.AddField("Reason:", reason)
					.SendEmbed(Context.Channel);
			}
			catch (HttpException)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Message failed!")
					.WithDescription($"The target user, {user.GetUserInformation()}, might have DMs disabled or might have blocked me... :c\nThe final warning has been recorded to the database regardless.")
					.SendEmbed(Context.Channel);
			}
			await InfractionsDB.SaveChangesAsync();
		}

		/// <summary>
		/// Revokes a final warning if an active one exists for target <paramref name="user"/>, and informs them of this.
		/// </summary>
		/// <param name="user">The user whose final warn is to be revoked.</param>
		/// <param name="reason">The reason why the final warn was revoked. (Optional)</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("delfinalwarn")]
		[Summary("Revokes a user's final warn, though it remains in records.")]
		[Alias("revokefinalwarn", "deletefinalwarn", "removefinalwarn")]
		[RequireModerator]
		[BotChannel]

		public async Task RevokeFinalWarn(IGuildUser user, [Remainder] string reason = "")
		{
			if (!TryRevokeFinalWarn(user, out FinalWarn warn))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("No active final warn found!")
					.WithDescription($"Wasn't able to revoke final warning for user {user.GetUserInformation()}, since no active warn exists.")
					.SendEmbed(Context.Channel);

				return;
			}

			try
			{
				if (warn.MessageID != 0)
					await (await (DiscordShardedClient.GetChannel(ModerationConfiguration.FinalWarningsChannelID) as ITextChannel).GetMessageAsync(warn.MessageID))?.DeleteAsync();
			}
			catch { }

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Final warn successfully revoked.")
				.WithDescription($"Successfully revoked final warning for user {user.GetUserInformation()}. You can still query records about this final warning.")
				.AddField(reason.Length > 0, "Reason:", reason)
				.SendEmbed(Context.Channel);

			try
			{
				await BuildEmbed(EmojiEnum.Love)
					.WithTitle("Your final warning has been revoked!")
					.WithDescription("The staff team has convened and decided to revoke your final warning. Be careful, you can't receive more than two final warnings! A third one is an automatic ban.")
					.AddField(reason.Length > 0, "Reason:", reason)
					.SendEmbed(await user.CreateDMChannelAsync());
			}
			catch (HttpException)
			{
				await Context.Channel.SendMessageAsync("This user either has closed DMs or has me blocked! I wasn't able to inform them of this.");
			}
			await InfractionsDB.SaveChangesAsync();
		}

		/// <summary>
		/// Gets the information for a final warn attached to <paramref name="user"/>, if any.
		/// </summary>
		/// <param name="user">The user to query for in the final warnings database.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("getfinalwarn")]
		[Summary("Gets the relevant information for a user's final warn.")]
		[Alias("queryfinalwarn")]
		[RequireModerator]
		[BotChannel]

		public async Task GetFinalWarn(IGuildUser user)
		{
			FinalWarn warn = FinalWarnsDB.FinalWarns.Find(user.Id);

			if (warn == null)
			{
				await BuildEmbed(EmojiEnum.Wut)
					.WithTitle("Target user is not under a final warning!")
					.WithDescription($"User {user.GetUserInformation()} has no final warnings to their name!")
					.SendEmbed(Context.Channel);

				return;
			}

			await BuildEmbed(EmojiEnum.Sign)
				.WithTitle("Final warning found!")
				.WithDescription($"User {user.GetUserInformation()} has {(warn.EntryType == EntryType.Revoke ? "a **revoked**" : "an **active**")} final warning!")
				.AddField("Reason:", warn.Reason)
				.AddField("Issued by:", DiscordShardedClient.GetUser(warn.IssuerID).GetUserInformation())
				.AddField("Mute Duration:", TimeSpan.FromSeconds(warn.MuteDuration).Humanize(), true)
				.AddField("Points Deducted:", warn.PointsDeducted, true)
				.AddField("Issued on:", DateTimeOffset.FromUnixTimeSeconds(warn.IssueTime).Humanize(), true)
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
