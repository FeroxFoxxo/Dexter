using System.Collections.Generic;
using System.Threading.Tasks;
using Dexter.Configurations;
using Dexter.Databases.CustomCommands;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;

namespace Dexter.Commands
{
	public partial class CustomCommands
	{
		/// <summary>
		/// Creates a new custom command tied to a specific user.
		/// </summary>
		/// <param name="cmdType">Either PATREON or STAFF, whatever ties the command to a user's permission.</param>
		/// <param name="name">The name of the command.</param>
		/// <param name="reply">The reply Dexter should put out when the command is run.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("mycommand")]
		[Summary("Usage: `mycommand <PATREON|STAFF> [cmdName] [reply]`")]
		[Alias("mycmd")]

		public async Task ManageUserCommand(string cmdType, string name, [Remainder] string reply)
		{
			name = name.ToLower();

			UserCommandSource cct = UserCommandSource.Unspecified;
			switch (cmdType.ToLower())
			{
				case "patreon":
					if (DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetUser(Context.User.Id).GetPatreonTier(DiscordShardedClient, BotConfiguration) < CustomCommandsConfiguration.MinimumPatreonTierForCustomCommands)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Insufficient Permissions")
							.WithDescription("You do not have the required patreon tier required to manage your own patreon command.")
							.SendEmbed(Context.Channel);
						return;
					}
					cct = UserCommandSource.Patreon;
					break;
				case "staff":
					if (DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetUser(Context.User.Id).GetPermissionLevel(DiscordShardedClient, BotConfiguration) < PermissionLevel.Moderator)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Insufficient Permissions")
							.WithDescription("You do not have the required staff role required to manage your own staff command.")
							.SendEmbed(Context.Channel);
						return;
					}
					cct = UserCommandSource.Staff;
					break;
				default:
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Invalid Command Type Expression")
						.WithDescription($"Unable to recognize command type \"{cmdType}\". Please use PATREON or STAFF.")
						.SendEmbed(Context.Channel);
					return;
			}

			if (reply.Length > CustomCommandsConfiguration.MaximumReplyLength)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Excessive Reply Length")
					.WithTitle($"The maximum length for a custom command reply is {CustomCommandsConfiguration.MaximumReplyLength}; your reply is {reply.Length} characters long.")
					.SendEmbed(Context.Channel);
				return;
			}

			CustomCommand incompatibleCC = CustomCommandDB.GetCommandByNameOrAlias(name);
			if (incompatibleCC is not null && incompatibleCC.User != Context.User.Id && incompatibleCC.CommandType != UserCommandSource.Unspecified)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Command Overlap")
					.WithDescription("A custom command that you don't control already exists with that name or an alias matching it!")
					.SendEmbed(Context.Channel);
				return;
			}

			StringBuilder description = new();
			description.Append($"User {Context.User.Mention} wants to set their custom {cct} command to {name}; with the following reply: \"{reply}\".");

			CustomCommand prevUC = GetCommandByUser(Context.User.Id, cct);
			if (prevUC is not null)
			{
				description.Append($"\nThis would replace the user command {prevUC.CommandName}; with the following reply: \"{prevUC.Reply.TruncateTo(512)}\"");
			}

			if (incompatibleCC is not null && incompatibleCC.CommandType == UserCommandSource.Unspecified)
			{
				description.Append($"\nThis would override the non-user-defined custom command {incompatibleCC.CommandName}; with the following reply: \"{incompatibleCC.Reply.TruncateTo(512)}\"");
			}

			Dictionary<string, string> setupArgs = new() {
							{ "CommandName", name },
							{ "Reply", reply },
							{ "CommandType", cct.ToString() },
							{ "User", Context.User.Id.ToString() }
			};

			await SendForAdminApproval(CreateCommandCallback,
					setupArgs,
					Context.User.Id,
					description.ToString());

			await BuildEmbed(EmojiEnum.Sign)
				.WithTitle($"The command `{BotConfiguration.Prefix}{name}` was suggested!")
				.WithDescription($"{description}\n" +
					$"Once it has passed admin approval, " +
					$"use `{BotConfiguration.Prefix}ccalias add {name} [alias]` to add an alias to the command! \n" +
					"Please note, to make the command ping a user if mentioned, add `USER` to the reply~! \n" +
					"To make the command ping the user who executes the command, add `AUTHOR` to the reply. \n" +
					$"To modify the reply at any time, use `{BotConfiguration.Prefix}ccedit`.")
				.SendEmbed(Context.Channel);

			await CustomCommandDB.SaveChangesAsync();
		}

		/// <summary>
		/// Ascertains whether a given <see cref="Databases.CustomCommands.CustomCommand"/> is currently eligible to be displayed.
		/// </summary>
		/// <param name="cc">The queried Custom Command.</param>
		/// <returns><see langword="true"/> if the command has a valid active user.</returns>

		public bool IsCustomCommandActive(CustomCommand cc)
		{
			return IsCustomCommandActive(cc, DiscordShardedClient, BotConfiguration, CustomCommandsConfiguration);
		}

		/// <summary>
		/// Ascertains whether a given <see cref="Databases.CustomCommands.CustomCommand"/> is currently eligible to be displayed.
		/// </summary>
		/// <param name="cc">The queried Custom Command.</param>
		/// <param name="client">The discord client required to obtain the role list of the user attached to the given command</param>
		/// <param name="botConfig">The botconfiguration required for patreon and staff roles</param>
		/// <param name="ccConfig">The custom command configuration required to know the patreon requirements to have an active command</param>
		/// <returns><see langword="true"/> if the command has a valid active user.</returns>
		public static bool IsCustomCommandActive(CustomCommand cc, DiscordShardedClient client, BotConfiguration botConfig, CustomCommandsConfiguration ccConfig)
		{
			if (cc.User == 0)
            {
                return true;
            }

            IGuildUser user = client.GetGuild(botConfig.GuildID).GetUser(cc.User);
			if (user is null)
            {
                return false;
            }

            return cc.CommandType switch
			{
				UserCommandSource.Patreon => user.GetPatreonTier(client, botConfig) >= ccConfig.MinimumPatreonTierForCustomCommands,
				UserCommandSource.Staff => user.GetPermissionLevel(client, botConfig) >= PermissionLevel.Moderator,
				_ => true
			};
		}
	}
}
