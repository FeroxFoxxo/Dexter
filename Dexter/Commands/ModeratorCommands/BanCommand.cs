using System;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.Infractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{

	public partial class ModeratorCommands
	{

		/// <summary>
		/// Bans the user from the current guild.
		/// </summary>
		/// <remarks>This command is staff-only.</remarks>
		/// <param name="user">The target user to ban from the guild.</param>
		/// <param name="reason">The reason for the ban.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("ban")]
		[Summary("Bans a user, assuming their account isn't Awoo or 1 week+ old. Bans if on Final Warning.")]
		[RequireModerator]

		public async Task BanUser (IUser user, string reason)
		{
			if (reason.Length > 1750)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to ban {user.Username}#{user.Discriminator}!")
					.WithDescription($"Your ban reason must be less than 1750. It currently stands at {reason.Length}.")
					.SendEmbed(Context.Channel);

				return;
			}

			var finalWarn = FinalWarnsDB.FinalWarns.Find(user.Id);

			if (finalWarn is null)
			{
				IGuildUser gUser = Context.Guild.GetUser(user.Id);

				if (gUser is not null)
				{
					string reasonForInability = string.Empty;

					if (gUser.RoleIds.Contains(GreetFurConfiguration.OwORole))
						reasonForInability = "The user has the OWO role";
					else if (DateTimeOffset.Now - gUser.JoinedAt > TimeSpan.FromDays(7))
						reasonForInability = "The user has been in the server for more than 7 days";

					if (!string.IsNullOrEmpty(reasonForInability))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle($"Unable to ban {user.Username}#{user.Discriminator}!")
							.WithDescription($"{reasonForInability}, which means they have to be final warned first!")
							.SendEmbed(Context.Channel);

						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle($"{user.Username}#{user.Discriminator} may need to be final warned!")
							.WithDescription($"{Context.User.GetUserInformation()} has attempted to ban {user.GetUserInformation()}. " +
								$"As such, it is likely they will need to be banned due to their recent rule breaks.")
							.AddField("Reason", reason.Length > 100 ? $"{reason.Substring(0, 100)}..." : reason)
							.SendEmbed(Context.Guild.GetTextChannel(BotConfiguration.ModerationLogChannelID));

						return;
					}
				}
			}
			else
			{
				FinalWarnsDB.Remove(finalWarn);
				await FinalWarnsDB.SaveChangesAsync();
			}

			await Context.Guild.AddBanAsync(user, reason: reason.Length > 100 ? reason.Substring(0, 100) : reason);

			int InfractionID = InfractionsDB.Infractions.Any() ? InfractionsDB.Infractions.Max(Warning => Warning.InfractionID) + 1 : 1;

			InfractionsDB.Infractions.Add(new Infraction()
			{
				Issuer = Context.User.Id,
				Reason = reason.Length > 100 ? $"{reason.Substring(0, 100)}..." : reason,
				User = user.Id,
				InfractionID = InfractionID,
				EntryType = EntryType.Issue,
				TimeOfIssue = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				PointCost = 5,
				InfractionTime = -1
			});

			await InfractionsDB.SaveChangesAsync();

			await BuildEmbed(EmojiEnum.Unknown)
				.WithTitle($"Banned {user.Username}#{user.Discriminator}.")
				.WithDescription($"Haiya! I've sucessfully banned {user.GetUserInformation()} from {Context.Guild.Name}.")
				.AddField("Reason", reason)
				.WithAuthor(user)
				.SendDMAttachedEmbed(Context.Channel, BotConfiguration, user,
					BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle($"You've been banned from {Context.Guild.Name}.")
						.WithDescription($"To be unbanned, please enter the ban appeal form, {ModerationConfiguration.BanAppealForm}.")
						.AddField("Reason", reason)
				);
		}

	}

}
