using System;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Humanizer;

namespace Dexter.Commands
{

	public partial class ModeratorCommands
	{

		/// <summary>
		/// The Issue Infraction method runs on WARN. It applies a warning to a user by adding it to the related database.
		/// It attaches this warning with a reason, and then notifies the recipient of the warning having been applied.
		/// This command can only be used by a moderator or higher position in the server.
		/// </summary>
		/// <param name="pointsDeducted">The number of points to deduct to the user's Dexter Profile for automoderation purposes.</param>
		/// <param name="user">The user of which you wish to warn.</param>
		/// <param name="reason">The reason for the user having been warned.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("warn")]
		[Summary("Issues a warning to a specified user.")]
		[ExtendedSummary("Issues a warning to a specified user. Syntax: `warn [POINTS] [USER] (options{OPTS...}) (REASON)`\n" +
			$"Possible options are: {Events.Moderation.InfractionOptions.OPTION_FORMATS}")]
		[Alias("warnUser")]
		[RequireModerator]

		public async Task IssueWarning(short pointsDeducted, IGuildUser user, [Remainder] string reason)
		{
			if (pointsDeducted == 1 || pointsDeducted == 2)
				await ModerationService.IssueInfraction(pointsDeducted, user, TimeSpan.FromSeconds(0), reason, Context);
			else
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Incorrect Infraction Type.")
					.WithDescription($"Haiya! All warning commands should either have 1 or 2 points deducted. What we found was {pointsDeducted} {(pointsDeducted == 1 ? "point" : "points")}. <3")
					.SendEmbed(Context.Channel);
		}

		/// <summary>
		/// The Issue Mute method runs on MUTE. It applies a mute to a user by adding it to the related database.
		/// It attaches this warning with a reason, and then notifies the recipient of the warning having been applied.
		/// This command can only be used by a moderator or higher position in the server and will mute the user for the set time.
		/// </summary>
		/// <param name="pointsDeducted">The number of points to subtract from the target user's Dexter Profile for automoderation purposes. Must be 0, 3 or 4.</param>
		/// <param name="user">The user of which you wish to mute.</param>
		/// <param name="time">The duration of the mute.</param>
		/// <param name="reason">The reason for the user having been mute.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("mute")]
		[Summary("Issues a mute to a specified user.")]
		[ExtendedSummary("Issues a mute to a specified user. Syntax: `mute [POINTS] [USER] [DURATION] (options{OPTS...}) (REASON)`\n" +
			$"Possible options are: {Events.Moderation.InfractionOptions.OPTION_FORMATS}")]
		[Alias("muteUser")]
		[RequireModerator]

		public async Task IssueMute(short pointsDeducted, IGuildUser user, TimeSpan time, [Remainder] string reason)
		{
			if (pointsDeducted is 3 or 4 or 0)
				await ModerationService.IssueInfraction(pointsDeducted, user, time, reason, Context);
			else
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Incorrect Infraction Type.")
					.WithDescription($"Haiya! All mute commands should either have 3 or 4 points deducted. What we found was {pointsDeducted} {(pointsDeducted == 1 ? "point" : "points")}. <3")
					.SendEmbed(Context.Channel);
		}

		/// <summary>
		/// Issues a mute to a specified user but does not add it to their records or affect their score.
		/// </summary>
		/// <remarks>This command should be used to issue non-punitive mutes only.</remarks>
		/// <param name="user"></param>
		/// <param name="time"></param>
		/// <param name="reason"></param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("mute")]
		[Summary("Issues a mute to a specified user. Does not add it to their records.")]
		[Alias("muteUser")]
		[RequireModerator]

		public async Task IssueMute(IGuildUser user, TimeSpan time, [Remainder] string reason)
		{
			await ModerationService.MuteUser(user, time);

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Unrecorded Mute Issued!")
				.WithDescription($"Muted {user.GetUserInformation()} for **{time.Humanize(2)}** due to `{reason}`. Please note that as this mute has not had a point count attached to it, it has not been recorded.")
				.SendDMAttachedEmbed(Context.Channel, BotConfiguration, user,
					BuildEmbed(EmojiEnum.Love)
						.WithTitle($"Unrecorded Mute Applied!")
						.WithDescription($"You have been muted in `{Context.Guild.Name}` for `{reason}` for a time of `{time.Humanize(2)}`. We hope you enjoy your time. <3")
	
				);
		}

		/// <summary>
		/// Issues an indefinite mute to a specified user.
		/// </summary>
		/// <param name="pointsDeducted">The number of points to remove from the user's Dexter profile. Must be set to 0.</param>
		/// <param name="user">Target user to mute.</param>
		/// <param name="reason">A string description of the reason why the mute was issued.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("mute")]
		[Summary("Issues an infinite mute to a specified user given the point amount of 0.")]
		[Alias("muteUser")]
		[RequireModerator]

		public async Task IssueMute(short pointsDeducted, IGuildUser user, [Remainder] string reason)
		{
			if (pointsDeducted == 0)
				await ModerationService.IssueInfraction(pointsDeducted, user, TimeSpan.FromSeconds(0), reason, Context);
			else
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Incorrect Infraction Type.")
					.WithDescription($"Haiya! Infinite mutes should have 0 points deducted. What we found was {pointsDeducted} {(pointsDeducted == 1 ? "point" : "points")}. <3")
					.SendEmbed(Context.Channel);
		}

	}

}
