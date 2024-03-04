using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.UserProfiles;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using System.Runtime.InteropServices;
using System.Text;

namespace Dexter.Commands
{

	public partial class UtilityCommands
	{

		/// <summary>
		/// Sends information concerning the profile of a target user.
		/// This information contains: Username, nickname, account creation and latest join date, and status.
		/// </summary>
		/// <param name="user">The target user</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("profile")]
		[Summary("Gets the profile of the user mentioned or yours.")]
		[Alias("userinfo")]
		[Priority(2)]
		[BotChannel]

		public async Task ProfileCommand([Optional] IUser user)
		{
			if (user == null)
				user = Context.User;

			IGuildUser guildUser = await DiscordShardedClient.Rest.GetGuildUserAsync(Context.Guild.Id, user.Id, new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
			if (guildUser is null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to fetch Guild User")
					.WithDescription($"The ID {(user is null ? "UNKNOWN_USER_ID" : user.Id.ToString())} has returned no valid Guild User from the Discord Rest API. {DiscordShardedClient.CurrentUser.Username} probably lacks access to the given user or the user is not in the server.")
					.SendEmbed(Context.Channel);
				return;
			}
			DateTimeOffset joined = await UserRecordsService.GetUserJoin(guildUser);

			UserProfile profile = ProfilesDB.Profiles.Find(user.Id);

			string[] lastNicknames = GetLastNameRecords(UserRecordsService.GetNameRecords(user, NameType.Nickname), 5);
			string[] lastUsernames = GetLastNameRecords(UserRecordsService.GetNameRecords(user, NameType.Username), 5);

			SocketRole roleInst = Context.Guild.Roles.Where(r => r.Position == Context.Guild.GetUser(user.Id).Hierarchy).FirstOrDefault();

			string role = roleInst != null ? roleInst.Name : "";

			await BuildEmbed(EmojiEnum.Unknown)
				.WithTitle($"User Profile For {guildUser.Username}")
				.WithThumbnailUrl(guildUser.GetTrueAvatarUrl())
				.AddField("Username", guildUser.GetUserInformation(), true)
				.AddField(!string.IsNullOrEmpty(guildUser.Nickname), "Nickname", guildUser.Nickname, true)
				.AddField("Created", $"{guildUser.CreatedAt:MM/dd/yyyy HH:mm:ss} ({(DateTime.Now - guildUser.CreatedAt.DateTime).Humanize(2, maxUnit: TimeUnit.Year)} ago)")
				.AddField(joined != default, "Joined", $"{joined:MM/dd/yyyy HH:mm:ss} ({DateTimeOffset.Now.Subtract(joined).Humanize(2, maxUnit: TimeUnit.Year)} ago)")
				.AddField(profile != null && profile.BorkdayTime != default, "Last Birthday", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(profile != null ? profile.BorkdayTime : 0).ToLongDateString())
				.AddField(!string.IsNullOrEmpty(role), "Top Role", role)
				.AddField(lastNicknames.Length > 1, $"Last {lastNicknames.Length} Nicknames:", string.Join(", ", lastNicknames))
				.AddField(lastUsernames.Length > 1, $"Last {lastUsernames.Length} Usernames:", string.Join(", ", lastUsernames))
				.SendEmbed(Context.Channel);
		}

		/// <summary>
		/// Sends information concerning the profile of a target user.
		/// This information contains: Username, nickname, account creation and latest join date, and status.
		/// </summary>
		/// <param name="userId">The target user's ID.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("profile")]
		[Summary("Gets the profile of the user mentioned by ID.")]
		[Alias("userinfo")]
		[Priority(1)]
		[BotChannel]

		public async Task ProfileCommand(ulong userId)
		{
			if (userId == 0)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Invalid ID")
					.WithDescription("The provided ID cannot be 0.")
					.SendEmbed(Context.Channel);
				return;
			}

			IUser u = await DiscordShardedClient.Rest.GetUserAsync(userId);

			if (u is null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("User not found")
					.WithDescription($"No User can be found with ID {userId}")
					.SendEmbed(Context.Channel);
				return;
			}

			await ProfileCommand(u);
		}

		private static string[] GetLastNameRecords(NameRecord[] FullArray, int MaxCount)
		{
			List<NameRecord> List = FullArray.ToList();
			List.Sort((a, b) => b.SetTime.CompareTo(a.SetTime));

			string[] Result = new string[Math.Min(List.Count, MaxCount)];

			for (int i = 0; i < Result.Length; i++)
			{
				Result[i] = List[i].Name;
			}

			return Result;
		}

		/// <summary>
		/// Gets a list of nicknames given a <paramref name="User"/>.
		/// </summary>
		/// <param name="User">The user whose nicknames are to be queried.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("nicknames")]
		[Summary("Gets the nicknames a user has had over time.")]
		[BotChannel]

		public async Task NicknamesCommand([Optional] IUser User)
		{
			await RunNamesCommands(User, NameType.Nickname);
		}

		/// <summary>
		/// Gets a list of usernames given a <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The user whose usernames are to be queried.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("usernames")]
		[Summary("gets the usernames a user has had over time.")]
		[BotChannel]

		public async Task UsernamesCommand([Optional] IUser user)
		{
			await RunNamesCommands(user, NameType.Username);
		}

		private async Task RunNamesCommands(IUser user, NameType nameType)
		{
			if (user == null)
				user = Context.User;

			List<NameRecord> names = UserRecordsService.GetNameRecords(user, nameType).ToList();
			names.Sort((a, b) => b.SetTime.CompareTo(a.SetTime));

			EmbedBuilder[] menu = BuildNicknameEmbeds(names.ToArray(), $"{nameType} Record for User {user.Username}");

			if (menu.Length == 1)
			{
				await menu[0].SendEmbed(Context.Channel);
			}
			else
			{
				await CreateReactionMenu(menu, Context.Channel);
			}
		}

		private const int MaxRowsPerEmbed = 16;

		private EmbedBuilder[] BuildNicknameEmbeds(NameRecord[] names, string title = "Names:")
		{
			int count = names.Length;
			EmbedBuilder[] result = new EmbedBuilder[(count - 1) / MaxRowsPerEmbed + 1];

			for (int p = 0; p < result.Length; p++)
			{
				StringBuilder content = new();

				foreach (NameRecord n in names[(p * MaxRowsPerEmbed)..((p + 1) * MaxRowsPerEmbed > count ? count : (p + 1) * MaxRowsPerEmbed)])
				{
					content.Append(n.Expression() + "\n");
				}

				result[p] = BuildEmbed(EmojiEnum.Sign)
					.WithTitle($"{title} Page {p + 1}/{result.Length}")
					.WithDescription(content.Length == 0 ? "Nothing to see here, folks." : content.Remove(content.Length - 1, 1).ToString())
					.WithFooter($"{p + 1}/{result.Length}");
			}

			return result;
		}

		/// <summary>
		/// Removes Names from the database given a set of arguments to match for.
		/// </summary>
		/// <param name="user">The User to match for, only names whose UserID match this user's will be removed.</param>
		/// <param name="nameType">Whether to remove USERNAMEs or NICKNAMEs.</param>
		/// <param name="arguments">Optionally include parsing mode (reg or lit) plus the expression to look for in each specific name.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("clearnames")]
		[Summary("Removes certain nicknames from a user's record\n" +
			"Usage: `clearnames [User] <NICK|USER> (<LIT|REG>) [Name]`")]
		[ExtendedSummary("Removes certain nicknames from a user's record\n" +
			"Usage: `clearnames [User] <NICK|USER> (<LIT|REG>) [Name]`\n" +
			"- <NICK|USER> represents whether to remove nicknames or usernames." +
			"- <LIT|REG> represents whether to use the literal Name or interpret it as a regular expression (advanced). Interpreted as \"LIT\" if omitted.")]
		[BotChannel]
		[RequireModerator]

		public async Task ClearNamesCommand(IUser user, string nameType, [Remainder] string arguments)
		{
			NameType enumNameType;

			switch (nameType.ToLower())
			{
				case "nick":
				case "nickname":
					enumNameType = NameType.Nickname;
					break;
				case "user":
				case "username":
					enumNameType = NameType.Username;
					break;
				default:
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Unable to parse Name Type argument!")
						.WithDescription($"Couldn't parse expression \"{nameType}\", make sure you use either NICK or USER, optionally followed by NAME")
						.SendEmbed(Context.Channel);
					return;
			}

			bool isRegex = false;
			string term = arguments.Split(" ").FirstOrDefault().ToLower();
			string name;

			switch (term)
			{
				case "reg":
					isRegex = true;
					name = arguments[term.Length..].Trim();
					break;
				case "lit":
					name = arguments[term.Length..].Trim();
					break;
				default:
					name = arguments;
					break;
			}

			if (string.IsNullOrEmpty(name))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("You must provide a name!")
					.WithDescription("You didn't provide any name to remove!")
					.SendEmbed(Context.Channel);
				return;
			}

			List<NameRecord> removed = await UserRecordsService.RemoveNames(user, enumNameType, name, isRegex);

			if (removed.Count == 0)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("No names found following your query.")
					.WithDescription($"I wasn't able to find any name \"{name}\" for this user! Make sure what you typed is correctly capitalized.")
					.SendEmbed(Context.Channel);
				return;
			}
			else
			{
				removed.Sort((a, b) => a.Name.CompareTo(b.Name));

				await BuildEmbed(EmojiEnum.Love)
					.WithTitle("Names successfully deleted!")
					.WithDescription($"This user had {removed.Count} name{(removed.Count != 1 ? "s" : "")} following this pattern:\n" +
						$"{LanguageHelper.TruncateTo(string.Join(", ", removed).ToString(), 2000)}")
					.SendEmbed(Context.Channel);
				return;
			}

		}

	}

}
