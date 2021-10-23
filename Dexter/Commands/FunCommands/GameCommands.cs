using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Games;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using System.Text;
using Dexter.Abstractions;

namespace Dexter.Commands
{
	partial class FunCommands
	{

		/// <summary>
		/// Interface Command for the games system and game management.
		/// </summary>
		/// <param name="action">An action description of what to do in the system.</param>
		/// <param name="arguments">A set of arguments to give context to <paramref name="action"/>.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("game")]
		[Summary("Used to create, manage and join games! Refer to the extended summary for use of this command by using `~help game`.")]
		[ExtendedSummary("Creates and manages game sessions.\n" +
			"`<NEW|CREATE> [Game] ([Title] (; [Description]))` - Creates a new game instance and joins it as a Master.\n" +
			"`<HELP|INFO>` - Shows information for the game you're currently in.\n" +
			"`JOIN [GameID] (Password)` - Joins a game by its Game ID.\n" +
			"`LEAVE` - Leaves the game, if you're the master, this will also delete the session and kick all players.\n" +
			"`GET (GameID)` - Gets general info about a game session or yours.\n" +
			"`LEADERBOARD` - Displays the game leaderboard.\n" +
			"`LIST` - Displays a list of available games\n" +
			"`RESET` - Resets the game state.\n" +
			"`SAVE` - Forces a database save of the Dexter Games system.\n" +
			"`SET [Field] [Value]` - Sets a value for your game. \n" +
			"-  Common Fields are `password`, `title`, `master`, and `description`\n" +
			"-  Certain games have special fields, such as `term` or `maxlives` in Hangman.")]
		[Alias("games")]
		[BotChannel]

		public async Task GameCommand(string action, [Remainder] string arguments = "")
		{
			if (RestrictionsDB.IsUserRestricted(Context.User, Databases.UserRestrictions.Restriction.Games) && action.ToLower() != "leave")
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("You aren't permitted to manage or join games!")
					.WithDescription("You have been blacklisted from using this service. If you think this is a mistake, feel free to personally contact an administrator.")
					.SendEmbed(Context.Channel);
				return;
			}

			string feedback;

			Player player = GamesDB.GetOrCreatePlayer(Context.User.Id);
			GameInstance game = null;
			if (player.Playing > 0)
			{
				game = GamesDB.Games.Find(player.Playing);
				if (game is not null && game.Type == GameType.Unselected) game = null;
			}

			switch (action.ToLower())
			{
				case "new":
				case "create":
					string[] args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					if (string.IsNullOrEmpty(args.FirstOrDefault()))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Invalid amount of parameters!")
							.WithDescription($"You must at least provide a game type! Use `{BotConfiguration.Prefix}game list` to see a list of available game types.")
							.SendEmbed(Context.Channel);
						return;
					}
					if (args.Length == 1) args = new string[2] { args[0], "Unnamed Session" };
					string gameTypeStr = args[0].ToLower();
					if (!GameTypeConversion.GameNames.ContainsKey(gameTypeStr))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Game not found!")
							.WithDescription($"Game \"{gameTypeStr}\" not found! Currently supported games are: {string.Join(", ", Enum.GetNames<GameType>()[1..])}")
							.SendEmbed(Context.Channel);
						return;
					}
					GameType gameType = GameTypeConversion.GameNames[gameTypeStr];
					string relevantArgs = arguments[args[0].Length..].Trim();
					string description = "";
					string title = relevantArgs.Trim();

					int separatorPos = relevantArgs.IndexOf(';');
					if (separatorPos + 1 == relevantArgs.Length) separatorPos = -1;
					if (separatorPos > 0)
					{
						title = relevantArgs[..separatorPos].Trim();
						description = relevantArgs[(separatorPos + 1)..].Trim();
					}

					if (string.IsNullOrEmpty(title)) title = $"{gameType} Game";

					game = OpenSession(player, title, description, gameType);
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle($"Created and Joined Game Session #{game.GameID}!")
						.WithDescription($"Created Game {game.Title}.\nCurrently playing {game.Type}")
						.SendEmbed(Context.Channel);
					return;
				case "help":
				case "info":
					if (game is null || game.ToGameProper(BotConfiguration) is null)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("You aren't in a game!")
							.WithDescription("Join or create a game before using this command.")
							.SendEmbed(Context.Channel);
						return;
					}
					await game.ToGameProper(BotConfiguration).Info(FunConfiguration).SendEmbed(Context.Channel);
					return;
				case "leaderboard":
				case "points":
					if (game is null)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("You aren't in a game!")
							.WithDescription("Join or create a game before using this command.")
							.SendEmbed(Context.Channel);
						return;
					}
					await Leaderboard(game).SendEmbed(Context.Channel);
					return;
				case "list":
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Supported Games!")
						.WithDescription($"**{string.Join("\n", Enum.GetNames<GameType>()[1..])}**")
						.SendEmbed(Context.Channel);
					return;
				case "join":
					if (string.IsNullOrEmpty(arguments))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("No Arguments Provided!")
							.WithDescription("You must provide, at least, an instance ID to join!")
							.SendEmbed(Context.Channel);
						return;
					}
					string number = arguments.Split(" ")[0];
					if (!int.TryParse(number, out int ID))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Failed to parse Game ID")
							.WithDescription($"Unable to parse {number} into an integer value.")
							.SendEmbed(Context.Channel);
						return;
					}
					if (game is not null && game.GameID == ID)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("You're already in this game!")
							.WithDescription("You can't join a game you're already in.")
							.SendEmbed(Context.Channel);
						return;
					}
					game = GamesDB.Games.Find(ID);
					if (!Join(player, game, out feedback, arguments[number.Length..].Trim()))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Failed to join game")
							.WithDescription(feedback)
							.SendEmbed(Context.Channel);
						return;
					}

					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Success!")
						.WithDescription(feedback)
						.SendEmbed(Context.Channel);
					return;
				case "leave":
					if (player is null || game is null)
					{
						await Context.Message.ReplyAsync("You're not in a game!");
						return;
					}
					RemovePlayer(player);

					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Left the game")
						.WithDescription($"You left Game {game.Title}. If you were the master, the session was closed.")
						.SendEmbed(Context.Channel);
					return;
				case "get":
				case "status":
					int gameID = -1;
					if (string.IsNullOrEmpty(arguments) || !int.TryParse(arguments, out gameID))
					{
						if (game is null)
						{
							await BuildEmbed(EmojiEnum.Annoyed)
								.WithTitle("Invalid selection")
								.WithDescription("You're not in a game and no game was specified!")
								.SendEmbed(Context.Channel);
							return;
						}
					}
					else
					{
						game = GamesDB.Games.Find(gameID);
					}

					if (game is null || game.ToGameProper(BotConfiguration) is null)
					{
						await Context.Message.ReplyAsync("This game doesn't exist or isn't active!");
						return;
					}
					await game.ToGameProper(BotConfiguration).GetStatus(DiscordShardedClient).SendEmbed(Context.Channel);
					return;
				case "reset":
					if (game is null || game.ToGameProper(BotConfiguration) is null)
					{
						await Context.Message.ReplyAsync("You're not in an implemented game!");
						return;
					}
					if (game.Master != Context.User.Id)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Missing permissions!")
							.WithDescription($"Only the game master (<@{game.Master}>) can reset the game!")
							.SendEmbed(Context.Channel);
						return;
					}
					game.ToGameProper(BotConfiguration).Reset(FunConfiguration, GamesDB);

					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Game successfully reset!")
						.WithDescription($"Reset Game {game.Title} (#{game.GameID}) to its default state.")
						.SendEmbed(Context.Channel);
					return;
				case "save":
					await Context.Message.ReplyAsync("Saved games!");
					return;
				case "set":
					if (string.IsNullOrEmpty(arguments))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("No Arguments Provided!")
							.WithDescription("You must provide a field and a value to set.")
							.SendEmbed(Context.Channel);
						return;
					}
					string field = arguments.Split(" ")[0];
					if (!Set(player, game, field, arguments[field.Length..].Trim(), out feedback))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Error!")
							.WithDescription(feedback)
							.SendEmbed(Context.Channel);
						return;
					}

					await BuildEmbed(EmojiEnum.Love)
						.WithTitle($"Changed the value of {field}")
						.WithDescription(string.IsNullOrEmpty(feedback) ? $"{field}'s value has been modified to \"{arguments[field.Length..].Trim()}\"" : feedback)
						.SendEmbed(Context.Channel);
					return;
			}
		}

		/// <summary>
		/// Manages players within the Dexter Games subsystem.
		/// </summary>
		/// <param name="action">What to do to <paramref name="user"/>, possible values as BAN, UNBAN, KICK, PROMOTE or SET</param>
		/// <param name="user">The target User representing the Player to perform <paramref name="action"/> on.</param>
		/// <param name="arguments">Any other relevant information for <paramref name="action"/>.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("player")]
		[Alias("players")]
		[Summary("Manages players in your game, use this command to kick, ban, promote, or set values of your players.")]
		[ExtendedSummary("Manages players in your game.\n" +
			"`BAN [PLAYER]` - Bans a player from your game.\n" +
			"`UNBAN [PLAYER]` - Removes a ban for a player.\n" +
			"`KICK [PLAYER]` - Kicks a player from your game, they can rejoin afterwards if they so desire.\n" +
			"`PROMOTE [PLAYER]` - Promotes a player to game master.\n" +
			"`SET [PLAYER] [FIELD] [VALUE]` - Sets a field for a player to a given value\n" +
			"-  Common fields are `score` and `lives`.")]
		[BotChannel]

		public async Task PlayerCommand(string action, IUser user, [Remainder] string arguments = "")
		{
			Player player = GamesDB.Players.Find(user.Id);
			if (player is null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("The player you targeted doesn't exist.")
					.WithDescription("Make sure the player you targeted is playing in your game session!")
					.SendEmbed(Context.Channel);
				return;
			}

			Player author = GamesDB.Players.Find(Context.User.Id);
			if (author is null)
			{
				await Context.Message.ReplyAsync("You aren't playing any games!");
				return;
			}
			GameInstance game = GamesDB.Games.Find(author.Playing);
			if (game is null || game.Master != Context.User.Id)
			{
				await Context.Message.ReplyAsync("You must be a master in an active game to manage players!");
				return;
			}

			switch (action.ToLower())
			{
				case "ban":
					if (!BanPlayer(player, game))
					{
						await Context.Message.ReplyAsync("This user is already banned from your game!");
						return;
					}
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Ban Registered")
						.WithDescription($"Player {user.Mention} has been banned from {game.Title}.")
						.AddField("Banned Players", game.BannedMentions.TruncateTo(500))
						.SendEmbed(Context.Channel);
					return;
				case "unban":
					if (!UnbanPlayer(user.Id, game))
					{
						await Context.Message.ReplyAsync("This player isn't banned in your game!");
						return;
					}
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Unbanned Player")
						.WithDescription($"Player {user.Mention} has been unbanned from {game.Title}.")
						.AddField(!string.IsNullOrWhiteSpace(game.Banned), "Banned Players", game.BannedMentions.TruncateTo(500))
						.SendEmbed(Context.Channel);
					return;
				case "kick":
					if (player.Playing != game.GameID)
					{
						await Context.Message.ReplyAsync("This player isn't in your game!");
						return;
					}
					RemovePlayer(player);
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Player successfully kicked")
						.WithDescription($"Player {user.Mention} has been kicked from {game.Title}.")
						.SendEmbed(Context.Channel);
					return;
				case "promote":
					await GameCommand("set", $"master {user.Id}");
					return;
				case "set":
					string[] args = arguments.Split(" ");
					if (args.Length < 2)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Invalid number of arguments!")
							.WithDescription("You must provide a field and a value to set it to.")
							.SendEmbed(Context.Channel);
						return;
					}
					if (!double.TryParse(args[1], out double value))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle("Cannot parse value")
							.WithDescription($"The term `{args[1]}` cannot be parsed to a number.")
							.SendEmbed(Context.Channel);
						return;
					}
					switch (args[0].ToLower())
					{
						case "lives":
							player.Lives = (int)value;
							await Context.Message.ReplyAsync($"Player {user.Mention} now has {value:D0} lives!");
							return;
						case "score":
						case "points":
							player.Score = value;
							await Context.Message.ReplyAsync($"Player {user.Mention} now has a score of {value:G3}!");
							return;
					}
					return;
			}
		}

		/// <summary>
		/// Forcefully removes a game instance.
		/// </summary>
		/// <param name="gameID">The gameID of the game instance to remove.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("killgame")]
		[Summary("Forces a game session to be closed by ID - Syntax `killgame [ID]`")]
		[RequireModerator]

		public async Task KillGameCommand(int gameID)
		{
			GameInstance game = GamesDB.Games.Find(gameID);
			if (game is null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to find game!")
					.WithDescription($"No game exists with ID {gameID}.")

					.SendEmbed(Context.Channel);
				return;
			}

			CloseSession(game);

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Game removed!")
				.WithDescription($"Game #{gameID} was successfully removed and all players in it were kicked.")
				.SendEmbed(Context.Channel);
			return;
		}

		private void RemovePlayer(ulong playerID)
		{
			Player player = GamesDB.Players.Find(playerID);

			if (player is null) return;
			RemovePlayer(player);
		}

		private async void RemovePlayer(Player player, bool save = true)
		{
			int wasPlaying = player.Playing;

			player.Score = 0;
			player.Lives = 0;
			player.Data = "";
			player.Playing = -1;

			if (!CheckCloseSession(wasPlaying) && save) 
				GamesDB.SaveChanges();
		}

		private bool CheckCloseSession(int instanceID)
		{
			GameInstance instance = GamesDB.Games.Find(instanceID);
			if (instance is null) return false;
			Player master = GamesDB.GetOrCreatePlayer(instance.Master);
			if (master.Playing != instanceID)
			{
				CloseSession(instance);
				return true;
			}
			return false;
		}

		private bool BanPlayer(Player player, GameInstance instance)
		{
			if (player is not null && player.Playing == instance.GameID)
			{
				RemovePlayer(player);
			}

			if (instance.Banned.Split(", ").Contains(player.UserID.ToString())) return false;
			instance.Banned += (instance.Banned.TrimEnd().Length > 0 ? ", " : "")
				+ player.UserID.ToString();
			return true;
		}

		private static bool UnbanPlayer(ulong playerID, GameInstance instance)
		{
			if (instance is null) return false;

			List<string> b = instance.Banned.Split(", ").ToList();
			if (!b.Contains(playerID.ToString())) return false;
			b.Remove(playerID.ToString());
			instance.Banned = string.Join(", ", b);
			return true;
		}

		private void CloseSession(GameInstance instance)
		{
			Player[] players = GamesDB.GetPlayersFromInstance(instance.GameID);

			foreach (Player p in players)
			{
				RemovePlayer(p, false);
			}

			GamesDB.Games.Remove(instance);
			GamesDB.SaveChanges();
		}

		private GameInstance OpenSession(Player master, string title, string description, GameType gameType)
		{
			RemovePlayer(master);

			GameInstance result = new()
			{
				GameID = 0,
				Master = master.UserID,
				Title = title,
				Description = description,
				Type = gameType,
				Banned = "",
				Data = "",
				LastInteracted = DateTimeOffset.Now.ToUnixTimeSeconds(),
				LastUserInteracted = master.UserID,
				Password = ""
			};

			GamesDB.Add(result);
			Join(master, result, out _, result.Password, false);

			GameTemplate game = result.ToGameProper(BotConfiguration);
			if (game is not null) game.Reset(FunConfiguration, GamesDB);

			GamesDB.SaveChanges();
			return result;
		}

		private bool Set(Player player, GameInstance instance, string field, string value, out string feedback)
		{
			if (player is null)
			{
				feedback = "You are not registered in any game! Join a game before you attempt to set a value.";
				return false;
			}

			if (instance is null)
			{
				feedback = "You are not registered in any game! Join a game before you attempt to set a value.";
				return false;
			}

			if (Context.User.Id != instance.Master)
			{
				feedback = "You are not this game's Master! You can't modify its values.";
				return false;
			}

			switch (field.ToLower())
			{
				case "title":
					instance.Title = value.Trim();
					feedback = $"Success! Title is \"{value}\"";
					break;
				case "desc":
				case "description":
					instance.Description = value.Trim();
					feedback = $"Success! Description is \"{value}\"";
					break;
				case "password":
					instance.Password = value.Trim();
					feedback = $"Success! Password is \"{value}\"";
					break;
				case "master":
					ulong id;
					IUser master;
					if (Context.Message.MentionedUsers.Count > 0)
					{
						master = Context.Message.MentionedUsers.First();
						id = master.Id;
					}
					else if (ulong.TryParse(value, out id))
					{
						master = DiscordShardedClient.GetUser(id);
						if (master is null)
						{
							feedback = $"The ID provided ({id}) doesn't map to any user.";
							return false;
						}
					}
					else
					{
						feedback = $"Unable to parse a user from \"{value}\"!";
						return false;
					}
					Player p = GamesDB.Players.Find(master.Id);
					if (p is null || p.Playing != instance.GameID)
					{
						feedback = $"The player you targeted isn't playing in your game instance!";
						return false;
					}

					instance.Master = id;
					feedback = $"Success! Master set to {master.Mention}";
					break;
				default:
					GameTemplate game = instance.ToGameProper(BotConfiguration);
					if (game is null)
					{
						feedback = "Game mode is not set! This is weird... you should probably make a new game session";
						return false;
					}

					if (game.Set(field, value, FunConfiguration, out feedback))
						break;
					else
						return false;
			}

			GamesDB.SaveChanges();
			return true;
		}

		private EmbedBuilder Leaderboard(GameInstance instance)
		{
			StringBuilder board = new();
			List<Player> players = GamesDB.GetPlayersFromInstance(instance.GameID).ToList();
			players.Sort((a, b) => b.Score.CompareTo(a.Score));

			foreach (Player p in players)
			{
				IUser user = DiscordShardedClient.GetUser(p.UserID);
				if (user is null) continue;
				board.Append($"{(board.Length > 0 ? "\n" : "")}{user.Username.TruncateTo(16),-16}| {p.Score:G4}, ♥×{p.Lives}");
			}

			return BuildEmbed(EmojiEnum.Unknown)
				.WithTitle($"Leaderboard for {instance.Title}")
				.WithDescription($"`{board}`");
		}

		private bool Join(Player player, GameInstance instance, out string feedback, string password = "", bool save = true)
		{
			feedback = "";

			if (instance is null)
			{
				feedback = "Game Instance does not exist.";
				return false;
			}

			string[] bannedIDs = instance.Banned.Split(", ");
			if (!string.IsNullOrWhiteSpace(instance.Banned))
			{
				foreach (string s in bannedIDs)
				{
					if (ulong.Parse(s) == player.UserID)
					{
						feedback = "Player is banned from this game.";
						return false;
					}
				}
			}

			if (!string.IsNullOrEmpty(instance.Password) && password != instance.Password)
			{
				feedback = "Password is incorrect.";
				return false;
			}

			RemovePlayer(player);
			player.Playing = instance.GameID;

			if (save) GamesDB.SaveChanges();

			feedback = $"Joined {instance.Title} (Game #{instance.GameID})";
			return true;
		}

	}
}
