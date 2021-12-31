using System;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.Infractions;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{

	public partial class ModeratorCommands
	{

		/// <summary>
		/// Bans the user from the current guild if they meet a set of requirements.
		/// </summary>
		/// <remarks>This command is staff-only.</remarks>
		/// <param name="user">The target user to ban from the guild.</param>
		/// <param name="reason">The reason for the ban.</param>
		/// <returns>A <see cref="Task"/> object, which can be awaited until this method completes successfully.</returns>

		[Command("ban")]
		[Summary("Bans a user, assuming their account meets the instaban requirements or is on Final Warning.")]
		[RequireModerator]

		public async Task BanCommand (IUser user, [Remainder] string reason)
		{
			await BanUser(user, reason, false);
		}

		/// <summary>
		/// Bans the user from the current guild.
		/// </summary>
		/// <remarks>This command is staff-only.</remarks>
		/// <param name="user">The target user to ban from the guild.</param>
		/// <param name="reason">The reason for the ban.</param>
		/// <returns>A <see cref="Task"/> object, which can be awaited until this method completes successfully.</returns>

		[Command("forceban")]
		[Summary("Bans a user, regardless of their account conditions. Should be used exclusively for extremely severe infractions.")]
		[RequireModerator]

		public async Task ForceBanCommand(IUser user, [Remainder] string reason)
        {
			await BanUser(user, reason, true);
		}

		private async Task BanUser(IUser user, string reason, bool force = false)
        {
			if (reason.Length > 1750)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to ban user!")
					.WithDescription($"Your ban reason must be less than 1750. It currently stands at {reason.Length}.")
					.SendEmbed(Context.Channel);
				return;
			}

			var finalWarn = FinalWarnsDB.FinalWarns.Find(user.Id);

			if (finalWarn is null || finalWarn.EntryType == EntryType.Revoke)
			{
				IGuildUser gUser = Context.Guild.GetUser(user.Id);

				if (gUser is not null)
				{

					if (gUser.RoleIds.Contains(BotConfiguration.ModeratorRoleID))
                    {
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle($"Unable to ban user!")
							.WithDescription($"{user.Username}#{user.Discriminator} is a staff member!")
							.SendEmbed(Context.Channel);
						return;
                    }

					string reasonForInability = string.Empty;

					bool hasLevel = gUser.RoleIds.Contains(ModerationConfiguration.InstabanResistanceRole);
					bool hasLongevity = DateTimeOffset.Now - gUser.JoinedAt > TimeSpan.FromDays(ModerationConfiguration.InstabanMaximumDays);

					if (hasLevel && hasLongevity)
						reasonForInability = "The target account is too high-level and has been in the server for too long to be banned without a final warning.";
					else if (ModerationConfiguration.InstabanOnOneConditionMet)
						reasonForInability = string.Empty;
					else if (hasLevel)
						reasonForInability = "The target account's level is too high to be banned without a final warning.";
					else if (hasLongevity)
						reasonForInability = "The target account has been in the server for too long to be banned without a final warning.";

					if (!string.IsNullOrEmpty(reasonForInability) && !force)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle($"Unable to ban {user.Username}#{user.Discriminator}!")
							.WithDescription($"{reasonForInability}, which means they have to be final warned first! If you wish to skip this step anyway, use the `forceban` command instead.")
							.SendEmbed(Context.Channel);

						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle($"{user.Username}#{user.Discriminator} may need to be final warned!")
							.WithDescription($"{Context.User.GetUserInformation()} has attempted to ban {user.GetUserInformation()}. " +
								$"As such, it is likely they will need to be banned due to their recent rule breaks.")
							.AddField("Reason", reason.TruncateTo(100))
							.SendEmbed(Context.Guild.GetTextChannel(BotConfiguration.ModerationLogChannelID));

						return;
					}
				}
			}
			else
			{
				finalWarn.EntryType = EntryType.Revoke;
				await FinalWarnsDB.SaveChangesAsync();
			}

			await Context.Guild.AddBanAsync(user, reason: reason.TruncateTo(100));

			int infractionID = InfractionsDB.Infractions.Any() ? InfractionsDB.Infractions.Max(warning => warning.InfractionID) + 1 : 1;

			InfractionsDB.Infractions.Add(new Infraction()
			{
				Issuer = Context.User.Id,
				Reason = reason.TruncateTo(100),
				User = user.Id,
				InfractionID = infractionID,
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
						.WithDescription($"If you think this was made in error, you may submit an appeal to the ban appeal form in 30 days: {ModerationConfiguration.BanAppealForm}.")
						.AddField("Reason", reason)
				);
		}

	}

}
