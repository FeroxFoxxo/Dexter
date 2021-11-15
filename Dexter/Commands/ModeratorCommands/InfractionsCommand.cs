using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Infractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using System.Runtime.InteropServices;

namespace Dexter.Commands
{

	public partial class ModeratorCommands
	{
		const string HELP_SUMMARY = "Returns a record of infractions for a given user.\n" +
			"You can use options after the user identification to modify the behaviour of the command\n" +
			"`all` - Displays all infractions (as opposed to only recent ones)\n" +
			"`reverse` - Displays infractions in chronological order, as opposed to being most-recent-first.";

		/// <summary>
		/// Sends an embed with the records of infractions of a specified user.
		/// </summary>
		/// <remarks>If the user is different from <c>Context.User</c>, it is Staff-only.</remarks>
		/// <param name="userId">The target user to query.</param>
		/// <param name="options">The additional flag options used for specialized rendering of the records command.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("records")]
		[Summary("Returns a record of infractions for a set user based on their ID.")]
		[ExtendedSummary(HELP_SUMMARY)]
		[Alias("warnings", "record", "warns", "mutes")]
		[RequireModerator]
		[BotChannel]

		public async Task InfractionsCommand(ulong userId, [Remainder] string options = "")
		{
			IUser user = await DiscordShardedClient.Rest.GetUserAsync(userId);

			if (user == null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to parse user information!")
					.WithDescription($"The given user ID ({userId}) doesn't match any known user according to Discord's API.")
					.SendEmbed(Context.Channel);

				/* This should only occur if the user ID is invalid
				EmbedBuilder[] warnings = GetWarnings(userId, Context.User.Id, $"<@{userId}>", $"Unknown ({userId})", true);

				await CreateReactionMenu(warnings, Context.Channel);
				*/
			}
			else
				await InfractionsCommand(user, options);
		}

        /// <summary>
        /// The InfractionsCommand runs on RECORDS and will send a DM to the author of the message if the command is run in a bot
        /// channel and no user is specified of their own infractions. If a user is specified and the author is a moderator it will
        /// proceed to print out all the infractions of that specified member into the channel the command had been sent into.
        /// </summary>
        /// <param name="user">The User field specifies the user that you wish to get the infractions of.</param>
        /// <param name="options">The additional flag options used for specialized rendering of the records command.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("records")]
		[Summary("Returns a record of infractions for a set user or your own.")]
		[ExtendedSummary(HELP_SUMMARY)]
		[Alias("warnings", "record", "warns", "mutes")]
		[BotChannel]

		public async Task InfractionsCommand([Optional] IUser user, [Remainder] string options = "")
		{
			bool isUserSpecified = user != null;
			RecordsFlags flags = RecordsFlags.None;
			List<string> unknownOptions = new();

			foreach(string s in options.ToLower().Split(' '))
            {
				switch(s)
                {
					case "all":
					case "showall":
						flags |= RecordsFlags.ShowAll;
						break;
					case "reverse":
					case "revert":
					case "invert":
					case "chronological":
					case "oldestfirst":
						flags |= RecordsFlags.Reverse;
						break;
					default:
						unknownOptions.Add(s);
						break;
                }
            }

			if (isUserSpecified)
			{
				if ((Context.User as IGuildUser).GetPermissionLevel(DiscordShardedClient, BotConfiguration) >= PermissionLevel.Moderator)
					await CreateReactionMenu(GetWarnings(user.Id, Context.User.Id, user.Mention, user.Username, flags | RecordsFlags.ShowIssuer), Context.Channel);
				else
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Halt! Don't go there-")
						.WithDescription("Heya! To run this command with a specified, you will need to be a moderator. <3")
						.SendEmbed(Context.Channel);
				}
			}
			else
			{
				try
				{
					if (Context.Channel is not SocketDMChannel)
					{
						await BuildEmbed(EmojiEnum.Love)
							.WithTitle("Sent infractions log.")
							.WithDescription("Heya! I've sent you a log of your infractions. Feel free to take a look over them in your own time! <3")
							.SendEmbed(Context.Channel);
					}

					await CreateReactionMenu(GetWarnings(Context.User.Id, Context.User.Id, Context.User.Mention, Context.User.Username, flags & ~RecordsFlags.ShowIssuer), await Context.User.CreateDMChannelAsync());
				}
				catch (HttpException)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Unable to send infractions log!")
						.WithDescription("Woa, it seems as though I'm not able to send you a log of your infractions! " +
							"This is usually indicitive of having DMs from the server blocked or me personally. " +
							"Please note, for the sake of transparency, we often use Dexter to notify you of events that concern you - " +
							"so it's critical that we're able to message you through Dexter. <3")
						.SendEmbed(Context.Channel);
				}
			}
		}

		/// <summary>
		/// The GetWarnings method returns an array of embeds detailing the user's warnings, time of warning, and moderator (if enabled).
		/// </summary>
		/// <param name="user">The user whose warnings you wish to receive.</param>
		/// <param name="runBy">The user who has run the given warnings command.</param>
		/// <param name="mention">The stringified mention for the target user.</param>
		/// <param name="username">The target user's username in the given context.</param>
		/// <param name="flags">Configures extra parameters for the request.</param>
		/// <returns>An array of embeds containing the given user's warnings.</returns>

		public EmbedBuilder[] GetWarnings(ulong user, ulong runBy, string mention, string username, RecordsFlags flags)
		{
			Infraction[] infractions = InfractionsDB.GetInfractions(user);

			if (infractions.Length <= 0)
				return new EmbedBuilder[1] {
					BuildEmbed(EmojiEnum.Love)
						.WithTitle("No issued infractions!")
						.WithDescription($"{mention} has a clean slate!\n" +
						$"Go give {(user == runBy ? "yourself" : "them")} a pat on the back. <3")
				};

			int totalInfractions = infractions.Length;
			int hiddenInfractions = 0;

			if (flags.HasFlag(RecordsFlags.ShowAll))
			{
				long tnow = DateTimeOffset.Now.ToUnixTimeSeconds();
				infractions = infractions.Where(i => tnow - i.TimeOfIssue <= ModerationConfiguration.RecentInfractionThreshold).ToArray();
				hiddenInfractions = totalInfractions - infractions.Length;
            }

			if (!flags.HasFlag(RecordsFlags.Reverse))
			{
				infractions = infractions.Reverse().ToArray();
			}

			List<EmbedBuilder> embeds = new();

			DexterProfile dexterProfile = InfractionsDB.GetOrCreateProfile(user);

			EmbedBuilder currentBuilder = BuildEmbed(EmojiEnum.Love)
				.WithTitle($"{username}'s Infractions - {infractions.Length} {(infractions.Length == 1 ? "Entry" : "Entries")} and {dexterProfile.InfractionAmount} {(dexterProfile.InfractionAmount == 1 ? "Point" : "Points")}.")
				.WithDescription($"All times are displayed in {TimeZoneInfo.Local.DisplayName}"
					+ (hiddenInfractions == 0 ? "" : $"\n(Hiding {hiddenInfractions} old infractions)"));

            for (int i = 0; i < infractions.Length; i++)
			{
				Infraction infraction = infractions[i];

				IUser issuer = Client.GetUser(infraction.Issuer);

				long timeOfIssue = infraction.TimeOfIssue;

				DateTimeOffset time = DateTimeOffset.FromUnixTimeSeconds(timeOfIssue);

				EmbedFieldBuilder field = new EmbedFieldBuilder()
					.WithName(
						(
						infraction.PointCost == 5 ?
							"Ban" :
							infraction.InfractionTime == 0 ? "Warning" :
							$"{TimeSpan.FromSeconds(infraction.InfractionTime).Humanize().Titleize()} Mute"
						)
						+ $" {i + 1} (ID {infraction.InfractionID})" +
						$"{(infraction.PointCost > 0 && infraction.PointCost < 5 ? $", - {infraction.PointCost} {(infraction.PointCost == 1 ? "Point" : "Points")}" : "")}."
					)
					.WithValue($"{(flags.HasFlag(RecordsFlags.ShowIssuer) ? $":cop: {(issuer != null ? issuer.GetUserInformation() : $"Unknown ({infraction.Issuer})")}\n" : "")}" +
						$":calendar: {time:M/d/yyyy h:mm:ss}\n" +
						$":notepad_spiral: {infraction.Reason}"
					);

				if (i % 5 == 0 && i != 0)
				{
					embeds.Add(currentBuilder);
					currentBuilder = BuildEmbed(EmojiEnum.Love).AddField(field);
				}
				else
				{
					try
					{
						currentBuilder.AddField(field);
					}
					catch (Exception)
					{
						embeds.Add(currentBuilder);
						currentBuilder = BuildEmbed(EmojiEnum.Love).AddField(field);
					}
				}
			}

			embeds.Add(currentBuilder);

			return embeds.ToArray();
		}

	}

	/// <summary>
	/// Determines the display mode of elements in the records command.
	/// </summary>

	[Flags]
	public enum RecordsFlags
    {
		/// <summary>
		/// Default setting
		/// </summary>
		None = 0,
		/// <summary>
		/// Displays the issuer of the infraction in the final embed. Should be reserved to moderator use.
		/// </summary>
		ShowIssuer = 1,
		/// <summary>
		/// Revert the order of the infractions so they show up in chronological order instead of most recent fist.
		/// </summary>
		Reverse = 2,
		/// <summary>
		/// Whether to display all infractions, if disabled, only infractions up to a certain time ago would be displayed.
		/// </summary>
		ShowAll = 4
    }

}
