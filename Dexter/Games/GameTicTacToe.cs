using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using System.Text;
using Dexter.Enums;
using Dexter.Abstractions;
using Dexter.Helpers;

namespace Dexter.Games
{

	/// <summary>
	/// Represents an instance of a tic tac toe game.
	/// </summary>

	public class GameTicTacToe : GameTemplate
	{

		private string StrState
		{
			get
			{
				return Game.Data.Split(", ")[0];
			}
			set
			{
				string[] NewValue = Game.Data.Split(", ");
				NewValue[0] = value;
				Game.Data = string.Join(", ", NewValue);
			}
		}

		private char[,] State
		{
			get
			{
				char[,] Result = new char[3, 3];
				string Raw = StrState;
				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < 3; j++)
						Result[i, j] = Raw[i * 3 + j];
				}
				return Result;
			}
			set
			{
				StringBuilder Builder = new(9);
				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < 3; j++)
						Builder.Append(value[i, j]);
				}
				StrState = Builder.ToString();
			}
		}

		private ulong BoardID
		{
			get
			{
				return ulong.Parse(Game.Data.Split(", ")[1]);
			}
			set
			{
				string ProcessedValue = value.ToString();
				string[] NewValue = Game.Data.Split(", ");
				NewValue[1] = ProcessedValue;
				Game.Data = string.Join(", ", NewValue);
			}
		}

		private ulong PlayerO
		{
			get
			{
				return ulong.Parse(Game.Data.Split(", ")[2]);
			}
			set
			{
				string ProcessedValue = value.ToString();
				string[] NewValue = Game.Data.Split(", ");
				NewValue[2] = ProcessedValue;
				Game.Data = string.Join(", ", NewValue);
			}
		}

		private ulong PlayerX
		{
			get
			{
				return ulong.Parse(Game.Data.Split(", ")[3]);
			}
			set
			{
				string ProcessedValue = value.ToString();
				string[] NewValue = Game.Data.Split(", ");
				NewValue[3] = ProcessedValue;
				Game.Data = string.Join(", ", NewValue);
			}
		}

		private char Turn
		{
			get
			{
				return Game.Data.Split(", ")[4][0];
			}
			set
			{
				string ProcessedValue = value.ToString();
				string[] NewValue = Game.Data.Split(", ");
				NewValue[4] = ProcessedValue;
				Game.Data = string.Join(", ", NewValue);
			}
		}

		/// <summary>
		/// Represents the general status and data of a Hangman Game.
		/// </summary>
		/// <param name="Client">SocketClient used to parse UserIDs.</param>
		/// <returns>An Embed detailing the various aspects of the game in its current instance.</returns>

		public override EmbedBuilder GetStatus(DiscordShardedClient Client)
		{
			return BuildEmbed(EmojiEnum.Unknown)
				.WithColor(Color.Blue)
				.WithTitle($"{Game.Title} (Game {Game.GameID})")
				.WithDescription($"{Game.Description}\n{DisplayState()}")
				.AddField($"{OChar} Player", $"{(PlayerO == default ? "-" : $"<@{PlayerO}>")}", true)
				.AddField($"{XChar} Player", $"{(PlayerX == default ? "-" : $"<@{PlayerX}>")}", true)
				.AddField($"Turn", $"{ToEmoji[Turn]}", true)
				.AddField("Master", Client.GetUser(Game.Master)?.GetUserInformation() ?? "<N/A>")
				.AddField(Game.Banned.Length > 0, "Banned Players", Game.BannedMentions.TruncateTo(500));
		}

		const string EChar = "⬜";
		const string OChar = "⭕";
		const string XChar = "❌";
		private readonly Dictionary<char, string> ToEmoji = new() {
			{'-', EChar},
			{'O', OChar},
			{'X', XChar}
		};

		private string DisplayState()
		{
			char[,] icons = State;
			StringBuilder builder = new();
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
					builder.Append(ToEmoji[icons[i, j]]);
				builder.Append('\n');
			}
			return builder.ToString();
		}

		/// <summary>
		/// Resets the game state to its initial default value.
		/// </summary>
		/// <param name="funConfiguration">Settings related to the fun module, which contain the default lives parameter.</param>
		/// <param name="gamesDB">The database containing player information, set to <see langword="null"/> to avoid resetting scores.</param>

		public override void Reset(FunConfiguration funConfiguration, GamesDB gamesDB)
		{
			Game.Data = EmptyData;
			Game.LastUserInteracted = Game.Master;
			if (gamesDB is null) return;
			Player[] Players = gamesDB.GetPlayersFromInstance(Game.GameID);
			foreach (Player p in Players)
			{
				p.Score = 0;
				p.Lives = 0;
			}
		}

		/// <summary>
		/// Sets a local <paramref name="field"/> to a given <paramref name="value"/>.
		/// </summary>
		/// <remarks>No Valid values for <paramref name="field"/>.</remarks>
		/// <param name="field">The name of the field to modify.</param>
		/// <param name="value">The value to set the field to.</param>
		/// <param name="funConfiguration">The Fun Configuration settings file, which holds relevant data such as default lives.</param>
		/// <param name="feedback">In case this operation wasn't possible, its reason, or useful feedback even if the operation was successful.</param>
		/// <returns><see langword="true"/> if the operation was successful, otherwise <see langword="false"/>.</returns>

		public override bool Set(string field, string value, FunConfiguration funConfiguration, out string feedback)
		{
			feedback = $"TicTacToe doesn't implement any special fields! {field} isn't a valid default field.";
			return false;
		}

		/// <summary>
		/// Gives general information about the game and how to play it.
		/// </summary>
		/// <param name="funConfiguration"></param>
		/// <returns></returns>

		public override EmbedBuilder Info(FunConfiguration funConfiguration)
		{
			return BuildEmbed(EmojiEnum.Unknown)
				.WithColor(Color.Magenta)
				.WithTitle("How To Play: TicTacToe")
				.WithDescription("**Step 1:** Create a new board by typing `board`.\n" +
					"**Step 2:** Claim your token, type `claim <O|X>` to claim circles or crosses respectively.\n" +
					"**Step 3:** `O` starts! type `(<O|X>) [POS]` to draw your token at a given position.\n" +
					"Positions are the following:\n" +
					"```\n" +
					"A3 B3 C3\n" +
					"A2 B2 C2\n" +
					"A1 B1 C1\n" +
					"```\n" +
					"Keep playing until you fill the board or get a line of three.\n" +
					"You can pass the token by typing `pass <O|X> [Player]` to give control of a token to another player (only the master or the player with that token can do this).\n" +
					"Alternatively, the master can type `swap` to swap player tokens.");
		}

		private bool PlaceToken(int x, int y, char token)
		{
			if (State[x, y] != '-') return false;
			char[] newState = StrState.ToCharArray();
			newState[x * 3 + y] = token;
			StrState = string.Join("", newState);
			return true;
		}

		private bool CheckWin()
		{
			char[,] state = State;
			for (int i = 0; i < 3; i++)
			{
				if (state[i, 0] != '-' && state[i, 0] == state[i, 1] && state[i, 1] == state[i, 2]) return true;
				if (state[0, i] != '-' && state[0, i] == state[1, i] && state[1, i] == state[2, i]) return true;
			}
			if (state[0, 0] != '-' && state[0, 0] == state[1, 1] && state[1, 1] == state[2, 2]) return true;
			if (state[0, 2] != '-' && state[0, 2] == state[1, 1] && state[1, 1] == state[2, 0]) return true;
			return false;
		}

		private bool CheckDraw()
		{
			char[,] state = State;
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
				{
					if (state[i, j] == '-') return false;
				}
			return true;
		}

		/// <summary>
		/// Handles a message sent by a player in the appropriate channel.
		/// </summary>
		/// <param name="message">The message context from which the author and content can be obtained.</param>
		/// <param name="gamesDB">The games database in case any player data has to be modified.</param>
		/// <param name="client">The Discord client used to parse users.</param>
		/// <param name="funConfiguration">The configuration file containing relevant game information.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public override async Task HandleMessage(IMessage message, GamesDB gamesDB, DiscordShardedClient client, FunConfiguration funConfiguration)
		{
			if (message.Channel is IDMChannel) return;
			Player player = gamesDB.GetOrCreatePlayer(message.Author.Id);

			string msg = message.Content.ToUpper().Replace("@", "@-");
			IUserMessage board = null;
			if (BoardID != 0) board = await message.Channel.GetMessageAsync(BoardID) as IUserMessage;

			if (msg == "BOARD")
			{
				if (board is not null) await board.DeleteAsync();
				IUserMessage newBoard = await message.Channel.SendMessageAsync(DisplayState());
				BoardID = newBoard.Id;
				return;
			}

			string[] args = msg.Split(" ");
			if (msg.StartsWith("CLAIM"))
			{
				if (args.Length > 1)
				{
					Player prevPlayer = null;
					bool skip = false;
					switch (args[1])
					{
						case "O":
							if (PlayerO == 0) skip = true;
							else prevPlayer = gamesDB.Players.Find(PlayerO);
							break;
						case "X":
							if (PlayerX == 0) skip = true;
							else prevPlayer = gamesDB.Players.Find(PlayerX);
							break;
						default:
							await message.Channel.SendMessageAsync($"\"{args[1]}\" is not a valid token! Use 'O' or 'X'.");
							return;
					}

					if (!skip && prevPlayer is not null && prevPlayer.Playing == Game.GameID)
					{
						await message.Channel.SendMessageAsync($"Can't claim token since player <@{prevPlayer.UserID}> is actively controlling it.");
						return;
					}

					if (args[1] == "O") PlayerO = message.Author.Id;
					else PlayerX = message.Author.Id;

					await message.Channel.SendMessageAsync($"<@{message.Author.Id}> will play with {(args[1] == "O" ? "circles" : "crosses")}!");
					return;
				}
				await message.Channel.SendMessageAsync("You need to specify what token you'd like to claim!");
				return;
			}

			if (args.Length > 0 && Positions.ContainsKey(args[0]))
			{
				args = new string[] {
					Turn.ToString(),
					args[0]
				};
			}

			if (args.Length > 1 && args[0] is "O" or "X")
			{
				if (board is null)
				{
					await message.Channel.SendMessageAsync($"You must create a board first! Type `board`");
					return;
				}

				if (Turn != args[0].First())
				{
					await message.Channel.SendMessageAsync($"It's not your turn!");
					return;
				}

				if ((args[0] == "O" && message.Author.Id != PlayerO) || (args[0] == "X" && message.Author.Id != PlayerX))
				{
					await message.Channel.SendMessageAsync($"You don't control this token!");
					return;
				}

				if (!Positions.ContainsKey(args[1]))
				{
					await message.Channel.SendMessageAsync($"Unable to parse position \"{args[1]}\"! Make sure you use a valid expression (see game info).");
					return;
				}
				Tuple<int, int> pos = Positions[args[1]];

				if (!PlaceToken(pos.Item1, pos.Item2, args[0].First()))
				{
					await message.Channel.SendMessageAsync($"This position is currently occupied!");
					return;
				}

				await message.DeleteAsync();
				await board.ModifyAsync(m => m.Content = DisplayState());
				Turn = Turn == 'O' ? 'X' : 'O';

				if (CheckWin())
				{
					BoardID = 0;
					StrState = "---------";
					Turn = 'O';
					player.Score += 1;
					await BuildEmbed(EmojiEnum.Unknown)
						.WithColor(Color.Green)
						.WithTitle($"{ToEmoji[args[0].First()]} wins!")
						.WithDescription("Create a new board if you wish to play again, or pass your token control to a different player.")
						.SendEmbed(message.Channel);
					return;
				}

				if (CheckDraw())
				{
					BoardID = 0;
					StrState = "---------";
					Turn = 'O';
					await BuildEmbed(EmojiEnum.Unknown)
						.WithColor(Color.LightOrange)
						.WithTitle("Draw!")
						.WithDescription("No more tokens can be placed. Create a new board if you wish to play again, or pass your token control to a different player.")
						.SendEmbed(message.Channel);
					return;
				}

				return;
			}

			if (args.Length > 2 && args[0] == "PASS")
			{
				ulong otherID = message.MentionedUserIds.FirstOrDefault();
				if (otherID == default && !ulong.TryParse(args[2], out otherID) || otherID == 0)
				{
					await message.Channel.SendMessageAsync($"Could not parse \"{args[2]}\" into a valid user.");
					return;
				}
				IUser otherUser = client.GetUser(otherID);
				if (otherUser is null)
				{
					await message.Channel.SendMessageAsync($"I wasn't able to find this user!");
					return;
				}
				Player otherPlayer = gamesDB.GetOrCreatePlayer(otherUser.Id);
				if (otherPlayer.Playing != Game.GameID)
				{
					await message.Channel.SendMessageAsync("That user isn't playing in this game session!");
					return;
				}

				switch (args[1])
				{
					case "O":
						if (message.Author.Id != Game.Master && message.Author.Id != PlayerO)
						{
							await message.Channel.SendMessageAsync("You aren't the master nor controlling the circle token!");
							return;
						}
						PlayerO = otherPlayer.UserID;
						break;
					case "X":
						if (message.Author.Id != Game.Master && message.Author.Id != PlayerX)
						{
							await message.Channel.SendMessageAsync("You aren't the master nor controlling the cross token!");
							return;
						}
						PlayerX = otherPlayer.UserID;
						break;
					default:
						await message.Channel.SendMessageAsync($"Unable to parse {args[1]} into a valid token!");
						return;
				}

				await message.Channel.SendMessageAsync($"{otherUser.Mention} now controls token {args[1]}!");
				return;
			}

			if (args[0] == "SWAP")
			{
				if (message.Author.Id != Game.Master)
				{
					await message.Channel.SendMessageAsync("Only the game master can swap the tokens!");
					return;
				}

				ulong temp = PlayerO;
				PlayerO = PlayerX;
				PlayerX = temp;
				await message.Channel.SendMessageAsync($"Tokens have been swapped!\n" +
					$"{OChar}: <@{PlayerO}>\n" +
					$"{XChar}: <@{PlayerX}>");
				return;
			}
		}

		private readonly Dictionary<string, Tuple<int, int>> Positions = new() {
			{ "A1", new Tuple<int, int>(2, 0) },
			{ "A2", new Tuple<int, int>(1, 0) },
			{ "A3", new Tuple<int, int>(0, 0) },
			{ "B1", new Tuple<int, int>(2, 1) },
			{ "B2", new Tuple<int, int>(1, 1) },
			{ "B3", new Tuple<int, int>(0, 1) },
			{ "C1", new Tuple<int, int>(2, 2) },
			{ "C2", new Tuple<int, int>(1, 2) },
			{ "C3", new Tuple<int, int>(0, 2) }
		};

		/// <summary>
		/// The initializer for the game class, setting both the instance information and the bot configuration.
		/// </summary>
		/// <param name="game">The current instance of the game.</param>
		/// <param name="botConfiguration">An instance of the bot's configuraiton.</param>

		//Data structure: "term, guess, lives, maxlives, lettersmissed";

		public GameTicTacToe(GameInstance game, BotConfiguration botConfiguration) : base(game, botConfiguration, "---------, 0, 0, 0, O")
		{
		}
	}
}
