using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using Dexter.Enums;
using Dexter.Abstractions;
using Dexter.Helpers;

namespace Dexter.Games
{

	/// <summary>
	/// Represents a game of Chess.
	/// </summary>

	public class GameChess : GameTemplate
	{
		private const string StartingPos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

		private string BoardRaw
		{
			get
			{
				return Game.Data.Split(", ")[0];
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[0] = value;
				Game.Data = string.Join(", ", newValue);
			}
		}

		private string LastMove
		{
			get
			{
				return Game.Data.Split(", ")[1];
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[1] = value;
				Game.Data = string.Join(", ", newValue);
			}
		}

		private ulong BoardID
		{
			get
			{
				return ulong.Parse(Game.Data.Split(", ")[2]);
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[2] = value.ToString();
				Game.Data = string.Join(", ", newValue);
			}
		}

		private ulong DumpID
		{
			get
			{
				return ulong.Parse(Game.Data.Split(", ")[3]);
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[3] = value.ToString();
				Game.Data = string.Join(", ", newValue);
			}
		}

		private ulong PlayerWhite
		{
			get
			{
				return ulong.Parse(Game.Data.Split(", ")[4]);
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[4] = value.ToString();
				Game.Data = string.Join(", ", newValue);
			}
		}

		private ulong PlayerBlack
		{
			get
			{
				return ulong.Parse(Game.Data.Split(", ")[5]);
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[5] = value.ToString();
				Game.Data = string.Join(", ", newValue);
			}
		}

		private string Agreements
		{
			get
			{
				return Game.Data.Split(", ")[6];
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[6] = value;
				Game.Data = string.Join(", ", newValue);
			}
		}

		private string Theme
		{
			get
			{
				return Game.Data.Split(", ")[7];
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[7] = value;
				Game.Data = string.Join(", ", newValue);
			}
		}

		private enum ViewMode
		{
			Flip,
			White,
			Black
		}

		private ViewMode View
		{
			get
			{
				return (ViewMode)int.Parse(Game.Data.Split(", ")[8]);
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[8] = ((int)value).ToString();
				Game.Data = string.Join(", ", newValue);
			}
		}

		private bool IsWhitesTurn
		{
			get
			{
				return BoardRaw.Split(" ")[1] == "w";
			}
		}

		/// <summary>
		/// Represents the general status and data of a Chess Game.
		/// </summary>
		/// <param name="client">SocketClient used to parse UserIDs.</param>
		/// <returns>An Embed detailing the various aspects of the game in its current instance.</returns>

		public override EmbedBuilder GetStatus(DiscordShardedClient client)
		{
			return BuildEmbed(EmojiEnum.Unknown)
				.WithColor(Discord.Color.Blue)
				.WithTitle($"{Game.Title} (Game {Game.GameID})")
				.WithDescription($"{Game.Description}")
				.AddField("White", $"<@{PlayerWhite}>", true)
				.AddField("Black", $"<@{PlayerBlack}>", true)
				.AddField("Turn", $"{(IsWhitesTurn ? "White" : "Black")}", true)
				.AddField("FEN Expression", BoardRaw)
				.AddField("Master", client.GetUser(Game.Master)?.GetUserInformation() ?? "<N/A>")
				.AddField(Game.Banned.Length > 0, "Banned Players", Game.BannedMentions.TruncateTo(500));
		}

		/// <summary>
		/// Prints information about how to play Chess.
		/// </summary>
		/// <param name="funConfiguration">The configuration file holding relevant information for the game.</param>
		/// <returns>An <see cref="EmbedBuilder"/> object holding the stylized information.</returns>

		public override EmbedBuilder Info(FunConfiguration funConfiguration)
		{
			return BuildEmbed(EmojiEnum.Unknown)
				.WithColor(Discord.Color.Magenta)
				.WithTitle("How to Play: Chess")
				.WithDescription("**Step 1.** Create a board by typing `board` in chat.\n" +
					"**Step 2.** Claim your colors! Type `claim <black|white>` to claim the pieces of a given color.\n" +
					"**Step 3.** White starts! Take turns moving pieces by typing moves in chat until an outcome is decided.\n" +
					"**[MORE INFO]**, for information on specific mechanics, type `info` followed by any of the following categories:\n" +
					$"{AuxiliaryInfo}\n" +
					"You can import a position from FEN notation using the `game set board [FEN]` command.\n" +
					"Standard rules of chess apply, you can resign with `resign` or offer a draw by typing `draw` in chat.\n" +
					"Once the game is complete, you can type `swap` to swap colors, or `pass [color] [player]` to give control of your pieces to someone else.");
		}

		const string AuxiliaryInfo = "> *moves* => Information about phrasing moves that can be understood by the engine.\n" +
			"> *view* => Information about changing the way the game is viewed graphically.\n" +
			"> *chess* => Information about how to play chess and useful resources for learning the game.\n" +
			"> *positions* => Information about additional custom starting positions that can be set using the `game set board [position]` command.";

		/// <summary>
		/// Resets all data, except player color control, if given a <paramref name="gamesDB"/>, it will also reset their score and lives.
		/// </summary>
		/// <param name="funConfiguration">The configuration file holding all relevant parameters for Games.</param>
		/// <param name="gamesDB">The games database where relevant data concerning games and players is stored.</param>

		public override void Reset(FunConfiguration funConfiguration, GamesDB gamesDB)
		{
			ulong white = PlayerWhite;
			ulong black = PlayerBlack;
			ulong dumpID = DumpID;
			Game.Data = EmptyData;
			PlayerWhite = white;
			PlayerBlack = black;
			DumpID = dumpID;
			if (gamesDB is not null)
			{
				foreach (Player p in gamesDB.GetPlayersFromInstance(Game.GameID))
				{
					p.Score = 0;
					p.Lives = 0;
				}
			}
		}

		/// <summary>
		/// Sets an internal game field <paramref name="field"/> to a given <paramref name="value"/>.
		/// </summary>
		/// <param name="field">The field to modify</param>
		/// <param name="value">The value to set <paramref name="field"/> to</param>
		/// <param name="funConfiguration">The configuration file holding relevant information about games settings.</param>
		/// <param name="feedback">The result of the operation, explained in a humanized way.</param>
		/// <returns><see langword="true"/> if the change was successful, otherwise <see langword="false"/>.</returns>

		public override bool Set(string field, string value, FunConfiguration funConfiguration, out string feedback)
		{
			switch (field.ToLower())
			{
				case "fen":
				case "pos":
				case "position":
				case "state":
				case "board":
					if (funConfiguration.ChessPositions.ContainsKey(value.ToLower()))
					{
						BoardRaw = funConfiguration.ChessPositions[value.ToLower()];
						feedback = $"Successfully reset the board to custom position: `{value}`.";
						return true;
					}
					if (!Board.TryParseBoard(value, out Board board, out feedback)) return false;
					BoardRaw = board.ToString();
					LastMove = "-";
					feedback = "Successfully set the value of board to the given value, type `board` to see the updated position.";
					return true;
				case "theme":
				case "style":
					if (!funConfiguration.ChessThemes.Contains(value.ToLower()))
					{
						feedback = $"Unable to find theme \"{value}\", valid themes are: {string.Join(", ", funConfiguration.ChessThemes)}.";
						return false;
					}
					Theme = value.ToLower();
					feedback = $"Successfully set theme to {value}";
					return true;
				case "view":
					switch (value.ToLower())
					{
						case "white":
							View = ViewMode.White;
							feedback = "View mode set to white! The game will be seen from white's perspective.";
							return true;
						case "black":
							View = ViewMode.Black;
							feedback = "View mode set to black! The game will be seen from black's perspective.";
							return true;
						case "flip":
							View = ViewMode.Flip;
							feedback = "View mode set to flip! The board will rotate for whoever's turn it is to play";
							return true;
						default:
							feedback = "Invalid view mode! Choose `white`, `black`, or `flip`.";
							return false;
					}
			}

			feedback = $"Invalid field: \"{field}\" is not a default field nor \"board\", \"theme\", or \"view\".";
			return false;
		}

		/// <summary>
		/// Handles a message sent in a games channel by a player currently playing in this game instance.
		/// </summary>
		/// <param name="message">The message sent by the player to be handled.</param>
		/// <param name="gamesDB">The database holding relevant information about games and players.</param>
		/// <param name="client">The Discord client used to send messages and parse users.</param>
		/// <param name="funConfiguration">The configuration settings attached to the Fun Commands module.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		public override async Task HandleMessage(IMessage message, GamesDB gamesDB, DiscordShardedClient client, FunConfiguration funConfiguration)
		{
			if (message.Channel is IDMChannel) return;
			Player player = gamesDB.GetOrCreatePlayer(message.Author.Id);

			string msgRaw = message.Content.Replace("@", "@-");
			string msg = msgRaw.ToLower();
			IUserMessage boardMsg = null;
			if (BoardID != 0) boardMsg = await message.Channel.GetMessageAsync(BoardID) as IUserMessage;

			if (!Board.TryParseBoard(BoardRaw, out Board board, out string boardCorruptedError))
			{
				await BuildEmbed(EmojiEnum.Unknown)
					.WithColor(Discord.Color.Red)
					.WithTitle("Corrupted Board State")
					.WithDescription($"Your current board state can't be parsed to a valid board, it has the following error:\n" +
						$"{boardCorruptedError}\n" +
						$"Feel free to reset the game using the `game reset` command or by setting a new board state in FEN notation with the `game set board [FEN]` command.")
					.SendEmbed(message.Channel);
				return;
			}

			if (msg == "board")
			{
				bool lastMoveValid = Move.TryParseMock(LastMove, board, out Move lastMove, out string lastMoveError);
				if (!lastMoveValid) lastMove = null;

				Outcome checkCalc = board.GetOutcome();
				if (checkCalc is Outcome.Check) lastMove.isCheck = true;
				if (checkCalc is Outcome.Checkmate) lastMove.isCheckMate = true;

				if (boardMsg is not null) await boardMsg.DeleteAsync();
				IUserMessage newBoard = await message.Channel.SendMessageAsync(await CreateBoardDisplay(board, lastMove, client, funConfiguration));
				BoardID = newBoard.Id;
				return;
			}

			string[] args = msg.Split(' ');
			if (args.Length > 0 && args[0] == "info")
			{
				if (args.Length == 1)
				{
					await BuildEmbed(EmojiEnum.Unknown)
						.WithColor(Discord.Color.Blue)
						.WithTitle("Auxiliary Information: Chess")
						.WithDescription($"Please specify one of the following categories when requesting information:\n{AuxiliaryInfo}")
						.SendEmbed(message.Channel);
					return;
				}

				switch (args[1])
				{
					case "moves":
						await BuildEmbed(EmojiEnum.Unknown)
							.WithColor(Discord.Color.Blue)
							.WithTitle("Auxiliary Information: Chess Moves")
							.WithDescription("Moves in chess can be expressed in one of two ways:\n" +
								"`[origin](x)[target]` - This represents a move from [origin] to [target], the piece is not specified.\n" +
								"Examples: Rook moves from a1 to d1: `a1d1`. Knight in b3 captures a piece on d4: `b3xd4`.\n" +
								"`(piece)(disambiguation)(x)[target]` - This represents the movement of a specific piece to a target square.\n" +
								"If no piece is provided, it's assumed you mean a pawn; disambiguation is required if multiple pieces of the same type could be moved to the same square.\n" +
								"Examples: Knight moves from a2 to c3: `Nc3`. Rook moves from a1 do d1, another rook is in e1: `Rad1`. Pawn on the e file captures in d3: `exd3`. Opening the game with pawn to d4: `d4`.\n" +
								"Castling is always notated as `O-O` for kingside castling and `O-O-O` for queenside castling.\n" +
								"Promotion to a non-queen can be specified by adding `=[Piece]` at the end of your pawn move: e.g. `e8=N`, `dxc1=R`." +
								"Note: The capture indicator is completely optional and ignored by the parser, it's not required or enforced.")
							.SendEmbed(message.Channel);
						return;
					case "view":
						await BuildEmbed(EmojiEnum.Unknown)
							.WithColor(Discord.Color.Blue)
							.WithTitle("Auxiliary Information: Chess Views")
							.WithDescription("There are two ways a game master can modify the way the game is viewed.\n" +
								"**View Mode**: If you don't like the board rotating every time it's the next player's turn, you can fix it to a specific side with the `game set view <flip|black|white>` command.\n" +
								"**Theme**: Getting bored of the way the pieces look? Change up their look a bit, explore our various themes for chess with the `game set theme [theme]` command.\n" +
								$"Currently supported themes are: {string.Join(", ", funConfiguration.ChessThemes)}.")
							.SendEmbed(message.Channel);
						return;
					case "chess":
					case "rules":
						await BuildEmbed(EmojiEnum.Unknown)
							.WithColor(Discord.Color.Blue)
							.WithTitle("Auxiliary Information: Chess Rules")
							.WithDescription("We heavily recommend you check out [this article on chess.com](https://www.chess.com/learn-how-to-play-chess) for an in-depth tutorial; but if you just want to read, here's a quick rundown.\n" +
								"The goal of the game is to put the enemy king in checkmate, meaning that it can't avoid capture in the next turn.\n" +
								"The **king** (K) moves one space in any direction, but can't move into a space that would put it in check!\n" +
								"The **queen** (Q) is the most powerful piece, it can move as many spaces as she wants in any direction.\n" +
								"The **rook** (R) moves orthogonally (horizontally or vertically) as many squares as it wants.\n" +
								"The **bishop** (B) moves diagonally as many squares as it wants, a bishop will always remain in the same square color.\n" +
								"The **knight** (N) moves in an L-shape, two squares in one direction, one in another. The knight is the only piece which can jump over other pieces\n" +
								"The **pawn** (P) is weird! It generally moves forward one square (except in the first move where it can move two), but it can ONLY capture diagonally. This means a pawn can't move if a piece is in front of it, unless it can capture a piece next to it.\n" +
								"Chess has a couple special moves, such as **castling** and **en passant**, these moves are a bit complicated, here are guides on [castling](https://www.youtube.com/watch?v=FcLYgXCkucc) and [en passant](https://www.youtube.com/watch?v=c_KRIH0wnhE).")
							.SendEmbed(message.Channel);
						return;
					case "positions":
						await BuildEmbed(EmojiEnum.Unknown)
							.WithColor(Discord.Color.Blue)
							.WithTitle("Auxiliary Information: Chess Positions")
							.WithDescription("Here are a couple predefined custom chess positions you can play.\n" +
								"To set your game to these positions, use the `game set board [position name]` command!\n" +
								$"Custom positions: **{string.Join("**, **", funConfiguration.ChessPositions.Keys)}**.")
							.SendEmbed(message.Channel);
						return;
					default:
						await BuildEmbed(EmojiEnum.Unknown)
							.WithColor(Discord.Color.Red)
							.WithTitle("Invalid Auxiliary Information Provided")
							.WithDescription("Please make sure to use the following categories: `moves`, `view`, or `chess`.")
							.SendEmbed(message.Channel);
						return;
				}
			}

			if (msg.StartsWith("claim"))
			{
				if (args.Length > 1)
				{
					Player prevPlayer = null;
					bool skip = false;
					switch (args[1])
					{
						case "white":
						case "w":
							if (PlayerWhite == 0) skip = true;
							else prevPlayer = gamesDB.Players.Find(PlayerWhite);
							break;
						case "black":
						case "b":
							if (PlayerBlack == 0) skip = true;
							else prevPlayer = gamesDB.Players.Find(PlayerBlack);
							break;
						default:
							await message.Channel.SendMessageAsync($"\"{args[1]}\" is not a valid color! Use 'B' or 'W'.");
							return;
					}

					if (!skip && prevPlayer is not null && prevPlayer.Playing == Game.GameID)
					{
						await message.Channel.SendMessageAsync($"Can't claim color since player <@{prevPlayer.UserID}> is actively controlling it.");
						return;
					}

					if (args[1].StartsWith("w")) PlayerWhite = message.Author.Id;
					else PlayerBlack = message.Author.Id;

					await message.Channel.SendMessageAsync($"<@{message.Author.Id}> will play with {(args[1].StartsWith("w") ? "white" : "black")}!");
					return;
				}
				await message.Channel.SendMessageAsync("You need to specify what color you'd like to claim!");
				return;
			}

			if (msg == "resign")
			{
				if (BoardRaw == StartingPos) return;
				bool resign = false;
				bool isWhite = false;
				if (message.Author.Id == PlayerWhite)
				{
					resign = true;
					isWhite = true;
					gamesDB.GetOrCreatePlayer(PlayerBlack).Score++;
				}
				else if (message.Author.Id == PlayerBlack)
				{
					resign = true;
					isWhite = false;
					gamesDB.GetOrCreatePlayer(PlayerWhite).Score++;
				}
				if (resign)
				{
					await BuildEmbed(EmojiEnum.Unknown)
						.WithColor(Discord.Color.Gold)
						.WithTitle($"{(isWhite ? "White" : "Black")} resigns!")
						.WithDescription($"The victory goes for {(isWhite ? "Black" : "White")}! The board has been reset, you can play again by typing `board`. The game master can swap colors by typing `swap`.")
						.SendEmbed(message.Channel);
					Reset(funConfiguration, gamesDB);
				}
			}

			if (msg == "draw")
			{
				if (BoardRaw == StartingPos) return;
				bool draw = false;
				bool isWhite = false;
				bool retracted = false;
				if (message.Author.Id == PlayerWhite)
				{
					draw = true;
					isWhite = true;
					if (Agreements[0] == 'D') { Agreements = $"N{Agreements[1]}"; retracted = true; }
					else Agreements = $"D{Agreements[1]}";
				}
				else if (message.Author.Id == PlayerBlack)
				{
					draw = true;
					isWhite = false;
					if (Agreements[1] == 'D') { Agreements = $"{Agreements[0]}N"; retracted = true; }
					else Agreements = $"{Agreements[0]}D";
				}
				if (draw)
				{
					if (Agreements == "DD")
					{
						await BuildEmbed(EmojiEnum.Unknown)
							.WithColor(Discord.Color.Orange)
							.WithTitle($"Draw!")
							.WithDescription($"No winners this game, but also no losers! The board has been reset, you can play again by typing `board`. The game master can swap colors by typing `swap`.")
							.SendEmbed(message.Channel);
						Reset(funConfiguration, gamesDB);
					}
					else
					{
						if (retracted)
						{
							await message.Channel.SendMessageAsync($"{(isWhite ? "White" : "Black")} retracts their offer to draw the game!");
						}
						else
						{
							await message.Channel.SendMessageAsync($"{(isWhite ? "White" : "Black")} is offering a draw, to accept it; type \"draw\"!");
						}
					}
				}
			}

			if (Move.TryParse(msgRaw, board, out Move move, out string error))
			{
				if (boardMsg is null)
				{
					await message.Channel.SendMessageAsync($"You must create a board first! Type `board`.");
					return;
				}

				if (!string.IsNullOrEmpty(error))
				{
					await message.Channel.SendMessageAsync(error);
					return;
				}

				if ((board.isWhitesTurn && message.Author.Id != PlayerWhite) || (!board.isWhitesTurn && message.Author.Id != PlayerBlack))
				{
					await message.Channel.SendMessageAsync($"You don't control the {(board.isWhitesTurn ? "white" : "black")} pieces!");
					return;
				}

				if (!move.IsLegal(board, out string legalerror))
				{
					await message.Channel.SendMessageAsync(legalerror);
					return;
				}

				board.ExecuteMove(move);
				Outcome outcome = board.GetOutcome();
				if (outcome is Outcome.Check) move.isCheck = true;
				if (outcome is Outcome.Checkmate) move.isCheckMate = true;
				BoardRaw = board.ToString();
				LastMove = move.ToString();
				await message.DeleteAsync();

				string link = await CreateBoardDisplay(board, move, client, funConfiguration);
				await boardMsg.ModifyAsync(m => m.Content = link);

				if (outcome == Outcome.Checkmate)
				{
					BoardID = 0;
					player.Score += 1;
					await BuildEmbed(EmojiEnum.Unknown)
						.WithColor(Discord.Color.Green)
						.WithTitle($"{(!board.isWhitesTurn ? "White" : "Black")} wins!")
						.WithDescription("Create a new board if you wish to play again, or pass your color control to a different player.")
						.SendEmbed(message.Channel);
					Reset(funConfiguration, null);
					return;
				}

				if (outcome == Outcome.Draw)
				{
					await BuildEmbed(EmojiEnum.Unknown)
						.WithColor(Discord.Color.LightOrange)
						.WithTitle("Draw!")
						.WithDescription($"Stalemate reached {(board.isWhitesTurn ? "White" : "Black")} has no legal moves but isn't in check. Create a new board if you wish to play again, or pass your color control to a different player.")
						.SendEmbed(message.Channel);
					Reset(funConfiguration, null);
					return;
				}

				if (outcome == Outcome.FiftyMoveRule)
				{
					await BuildEmbed(EmojiEnum.Unknown)
						.WithColor(Discord.Color.LightOrange)
						.WithTitle("Draw!")
						.WithDescription("50 moves went by without advancing a pawn or capturing a piece, the game is declared a draw. \nCreate a new board if you wish to play again, or pass your color control to a different player.")
						.SendEmbed(message.Channel);
					Reset(funConfiguration, null);
				}

				if (outcome == Outcome.InsufficientMaterial)
				{
					await BuildEmbed(EmojiEnum.Unknown)
						.WithColor(Discord.Color.LightOrange)
						.WithTitle("Draw!")
						.WithDescription("Neither player has sufficient material to deliver checkmate, the game is declared a draw. \nCreate a new board if you wish to play again, or pass your color control to a different player.")
						.SendEmbed(message.Channel);
					Reset(funConfiguration, null);
				}

				return;
			}

			if (args.Length > 2 && args[0] == "pass")
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
					case "w":
					case "white":
						if (message.Author.Id != Game.Master && message.Author.Id != PlayerWhite)
						{
							await message.Channel.SendMessageAsync($"You aren't the master nor controlling the white pieces!");
							return;
						}
						PlayerWhite = otherPlayer.UserID;
						break;
					case "b":
					case "black":
						if (message.Author.Id != Game.Master && message.Author.Id != PlayerBlack)
						{
							await message.Channel.SendMessageAsync($"You aren't the master nor controlling the black pieces!");
							return;
						}
						PlayerBlack = otherPlayer.UserID;
						break;
					default:
						await message.Channel.SendMessageAsync($"Unable to parse \"{args[1]}\" into a valid color!");
						return;
				}

				await message.Channel.SendMessageAsync($"{otherUser.Mention} now controls the {(args[1][0] == 'w' ? "white" : "black")} pieces!");
				return;
			}

			if (args[0] == "swap")
			{
				if (message.Author.Id != Game.Master)
				{
					await message.Channel.SendMessageAsync("Only the game master can swap the colors!");
					return;
				}

				ulong temp = PlayerWhite;
				PlayerWhite = PlayerBlack;
				PlayerBlack = temp;
				await message.Channel.SendMessageAsync($"Colors have been swapped!\n" +
					$"WHITE: <@{PlayerWhite}>\n" +
					$"BLACK: <@{PlayerBlack}>");
				return;
			}
		}

		private async Task<string> CreateBoardDisplay(Board board, Move lastMove, DiscordShardedClient client, FunConfiguration funConfiguration)
		{
			string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
			string filepath = Path.Join(imageChacheDir, $"Chess{Game.Master}.png");
			System.Drawing.Image image = RenderBoard(board, lastMove);
			image.Save(filepath);
			if (DumpID != 0)
			{
				IMessage prevDump = await (client.GetChannel(funConfiguration.GamesImageDumpsChannel) as ITextChannel).GetMessageAsync(DumpID);
				if (prevDump is not null) await prevDump.DeleteAsync();
			}
			IUserMessage cacheMessage = await (client.GetChannel(funConfiguration.GamesImageDumpsChannel) as ITextChannel).SendFileAsync(filepath);
			DumpID = cacheMessage.Id;

			return cacheMessage.Attachments.First().ProxyUrl;
		}

		private System.Drawing.Image RenderBoard(Board board, Move lastMove)
		{
			Bitmap img = new(2 * Offset + 8 * CellSize, 2 * Offset + 8 * CellSize);

			Dictionary<char, System.Drawing.Image> pieceImages = new();
			foreach (Piece p in Piece.pieces)
			{
				for (int c = 0; c < 2; c++)
				{
					pieceImages.Add(c == 0 ? p.representation : char.ToLower(p.representation),
						System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{PiecePrefixes[c]}{p.representation}.png")));
				}
			}

			bool whiteside = (View) switch
			{
				ViewMode.White => true,
				ViewMode.Black => false,
				_ => board.isWhitesTurn
			};

			using (Graphics g = Graphics.FromImage(img))
			{
				using (System.Drawing.Image boardImg = System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{BoardImgName}.png")))
				{
					if (!whiteside) boardImg.RotateFlip(RotateFlipType.Rotate180FlipNone);
					g.DrawImage(boardImg, 0, 0, 2 * Offset + 8 * CellSize, 2 * Offset + 8 * CellSize);
				}

				if (lastMove != null)
				{
					using (System.Drawing.Image highlight = System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{HighlightImage}.png")))
					{
						foreach (int n in lastMove.ToHighlight())
						{
							if (whiteside) g.DrawImage(highlight, Offset + (n % 8) * CellSize, Offset + (n / 8) * CellSize, CellSize, CellSize);
							else g.DrawImage(highlight, Offset + (7 - n % 8) * CellSize, Offset + (7 - n / 8) * CellSize, CellSize, CellSize);
						}
					}
					if (lastMove.isCapture)
					{
						if (lastMove.isEnPassant)
						{
							using System.Drawing.Image captureMark = System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{CaptureImage}.png"));
							foreach (int n in lastMove.ToEnPassant())
							{
								if (whiteside) g.DrawImage(captureMark, (n % 8) * CellSize, (n / 8) * CellSize, CellSize + 2 * Offset, CellSize + 2 * Offset);
								else g.DrawImage(captureMark, (7 - n % 8) * CellSize, (7 - n / 8) * CellSize, CellSize + 2 * Offset, CellSize + 2 * Offset);
							}
						}
						else
						{
							using System.Drawing.Image captureMark = System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{CaptureImage}.png"));
							if (whiteside) g.DrawImage(captureMark, (lastMove.target % 8) * CellSize, (lastMove.target / 8) * CellSize, CellSize + 2 * Offset, CellSize + 2 * Offset);
							else g.DrawImage(captureMark, (7 - lastMove.target % 8) * CellSize, (7 - lastMove.target / 8) * CellSize, CellSize + 2 * Offset, CellSize + 2 * Offset);
						}
					}

					using System.Drawing.Image danger = System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{DangerImage}.png"));
					foreach (int n in lastMove.ToDanger(board))
					{
						if (whiteside) g.DrawImage(danger, Offset + (n % 8) * CellSize, Offset + (n / 8) * CellSize, CellSize, CellSize);
						else g.DrawImage(danger, Offset + (7 - n % 8) * CellSize, Offset + (7 - n / 8) * CellSize, CellSize, CellSize);
					}
				}

				for (int x = 0; x < 8; x++)
				{
					for (int y = 0; y < 8; y++)
					{
						if (board.squares[x, y] != '-')
						{
							if (whiteside) g.DrawImage(pieceImages[board.squares[x, y]], Offset + CellSize * x, Offset + CellSize * y, CellSize, CellSize);
							else g.DrawImage(pieceImages[board.squares[x, y]], Offset + (7 - x) * CellSize, Offset + (7 - y) * CellSize, CellSize, CellSize);
						}
					}
				}
			}

			return img;
		}

		const int CellSize = 64;
		const int Offset = 32;
		private const string ChessPath = "Images/Games/Chess";
		private const string BoardImgName = "Board";
		private const string HighlightImage = "SquareHighlight";
		private const string CaptureImage = "CaptureMarker";
		private const string DangerImage = "SquareDanger";
		private readonly string[] PiecePrefixes = new string[] { "W", "B" };

		/// <summary>
		/// The initializer for the game class, setting both the instance information and the bot configuration.
		/// </summary>
		/// <param name="game">The current instance of the game.</param>
		/// <param name="botConfiguration">An instance of the bot's configuraiton.</param>
		public GameChess(GameInstance game, BotConfiguration botConfiguration) : base(game, botConfiguration, StartingPos + ", -, 0, 0, 0, 0, NN, standard, 0") {
		}

		private static Tuple<int, int> ToMatrixCoords(int pos)
		{
			return new Tuple<int, int>(pos % 8, pos / 8);
		}

		private static string ToSquareName(Tuple<int, int> coord)
		{
			return $"{(char)('a' + coord.Item1)}{(char)('8' - coord.Item2)}";
		}

		private static bool TryParseSquare(string input, out int pos)
		{
			pos = -1;
			if (input.Length < 2) return false;

			if (input[0] < 'a' || input[0] > 'h') return false;
			pos = input[0] - 'a';

			if (input[1] < '1' || input[1] > '8') return false;
			pos += ('8' - input[1]) * 8;

			return true;
		}

		[Flags]
		private enum MoveType
		{
			None = 0,
			Orthogonal = 1,
			Diagonal = 2,
			Direct = 4,
			Pawn = 8
		}

		private class Piece
		{
			public char representation;
			public string name;
			public bool canPin;
			public Func<int, int, Board, bool, bool> isValid;
			public Func<int, Board, bool, bool> hasValidMoves;

			public static readonly Piece Rook = new()
			{
				representation = 'R',
				name = "Rook",
				canPin = true,
				isValid = (origin, target, board, flip) =>
				{
					if (!BasicValidate(Rook, origin, target, board, flip)) return false;
					return OrthogonalValidate(origin, target, board);
				},
				hasValidMoves = (origin, board, flip) => HasOrthogonalValidMoves(origin, board, flip)
			};

			public static readonly Piece Knight = new()
			{
				representation = 'N',
				name = "Knight",
				canPin = false,
				isValid = (origin, target, board, flip) =>
				{
					if (!BasicValidate(Knight, origin, target, board, flip)) return false;
					int xdiff = Math.Abs(target % 8 - origin % 8);
					int ydiff = Math.Abs(target / 8 - origin / 8);
					if (xdiff == 0 || ydiff == 0) return false;
					return xdiff + ydiff == 3;
				},
				hasValidMoves = (origin, board, flip) =>
				{
					int x0 = origin % 8;
					int y0 = origin / 8;
					for (int dx = 1; dx <= 2; dx++)
					{
						for (int xsign = -1; xsign <= 1; xsign += 2)
						{
							int x = x0 + dx * xsign;
							if (x < 0 || x >= 8) continue;
							for (int ysign = -1; ysign <= 1; ysign += 2)
							{
								int y = y0 + (3 - dx) * ysign;
								if (y < 0 || y >= 8) continue;
								if (board.squares[x, y] == '-' || (char.IsUpper(board.squares[x, y]) ^ board.isWhitesTurn ^ flip)) return true;
							}
						}
					}
					return false;
				}
			};

			public static readonly Piece Bishop = new()
			{
				representation = 'B',
				name = "Bishop",
				canPin = true,
				isValid = (origin, target, board, flip) =>
				{
					if (!BasicValidate(Bishop, origin, target, board, flip)) return false;
					return DiagonalValidate(origin, target, board);
				},
				hasValidMoves = (origin, board, flip) => HasDiagonalValidMoves(origin, board, flip)
			};

			public static readonly Piece King = new()
			{
				representation = 'K',
				name = "King",
				canPin = false,
				isValid = (origin, target, board, flip) =>
				{
					if (!BasicValidate(King, origin, target, board, flip)) return false;
					int xdiff = Math.Abs(target % 8 - origin % 8);
					int ydiff = Math.Abs(target / 8 - origin / 8);
					return xdiff <= 1 && ydiff <= 1;
				},
				hasValidMoves = (origin, board, flip) => HasDiagonalValidMoves(origin, board, flip, true) || HasOrthogonalValidMoves(origin, board, flip, true)
			};

			public static readonly Piece Queen = new()
			{
				representation = 'Q',
				name = "Queen",
				canPin = true,
				isValid = (origin, target, board, flip) =>
				{
					if (!BasicValidate(Queen, origin, target, board, flip)) return false;
					return OrthogonalValidate(origin, target, board) || DiagonalValidate(origin, target, board);
				},
				hasValidMoves = (origin, board, flip) => HasDiagonalValidMoves(origin, board, flip) || HasOrthogonalValidMoves(origin, board, flip)
			};

			public static readonly Piece Pawn = new()
			{
				representation = 'P',
				name = "Pawn",
				canPin = false,
				isValid = (origin, target, board, flip) =>
				{
					if (!BasicValidate(Pawn, origin, target, board, flip)) return false;
					int yAdv = (board.isWhitesTurn ^ flip) ? -1 : 1;
					int x0 = origin % 8;
					int y0 = origin / 8;
					int xf = target % 8;
					int yf = target / 8;
					if (board.GetSquare(target) != '-' || (target == board.enPassant && x0 != xf))
					{
						return Math.Abs(x0 - xf) == 1 && yf == y0 + yAdv;
					}
					else
					{
						if (xf != x0) return false;
						if (yf == y0 + yAdv) return true;
						int initialRank = (board.isWhitesTurn ^ flip) ? 6 : 1;
						return y0 == initialRank && yf == y0 + 2 * yAdv && board.squares[x0, y0 + yAdv] == '-';
					}
				},
				hasValidMoves = (origin, board, flip) =>
				{
					int yAdv = (board.isWhitesTurn ^ flip) ? -1 : 1;
					int x0 = origin % 8;
					int y0 = origin / 8;
					if (y0 + yAdv < 0 || y0 + yAdv >= 8) return false;
					if (board.squares[x0, y0 + yAdv] == '-') return true;
					for (int x = x0 - 1; x <= x0 + 1; x += 2)
					{
						if (x < 0 || x >= 8) continue;
						if (board.enPassant == x + (y0 + yAdv) * 8) return true;
						if (board.squares[x, y0 + yAdv] == '-') continue;
						if (char.IsUpper(board.squares[x, y0 + yAdv]) ^ board.isWhitesTurn ^ flip) return true;
					}
					return false;
				}
			};

			private static bool BasicValidate(Piece p, int origin, int target, Board board, bool flip)
			{
				if (origin == target) return false;
				if (char.ToUpper(board.GetSquare(origin)) != p.representation) return false;
				char piecef = board.GetSquare(target);
				if (piecef != '-' && (char.IsLower(piecef) ^ board.isWhitesTurn ^ flip)) return false;
				return true;
			}

			private static bool OrthogonalValidate(int origin, int target, Board board)
			{
				int x0 = origin % 8;
				int y0 = origin / 8;
				int xf = target % 8;
				int yf = target / 8;
				if (y0 == yf)
				{
					int direction = target - origin > 0 ? 1 : -1;
					int x = x0 + direction;
					while (x != xf)
					{
						if (board.squares[x, y0] != '-') return false;
						x += direction;
					}
					return true;
				}
				else if (x0 == xf)
				{
					int direction = target - origin > 0 ? 1 : -1;
					int y = y0 + direction;
					while (y != yf)
					{
						if (board.squares[x0, y] != '-') return false;
						y += direction;
					}
					return true;
				}

				return false;
			}

			private static bool DiagonalValidate(int origin, int target, Board board)
			{
				int x0 = origin % 8;
				int y0 = origin / 8;
				int xf = target % 8;
				int yf = target / 8;
				int ydir = target - origin > 0 ? 1 : -1;
				int xdir;
				if (x0 + y0 == xf + yf)
				{
					xdir = -ydir;
				}
				else if (x0 - y0 == xf - yf)
				{
					xdir = ydir;
				}
				else return false;

				int x = x0 + xdir;
				int y = y0 + ydir;
				while (x != xf)
				{
					if (board.squares[x, y] != '-') return false;
					x += xdir;
					y += ydir;
				}

				return true;
			}

			private static bool HasOrthogonalValidMoves(int origin, Board board, bool flip, bool mustBeSafe = false)
			{
				int x0 = origin % 8;
				int y0 = origin / 8;
				char p;
				char thisPiece = board.squares[x0, y0];
				for (int x = x0 - 1; x <= x0 + 1; x += 2)
				{
					if (x < 0 || x >= 8) continue;
					p = board.squares[x, y0];
					if (p != '-' && !(char.IsUpper(p) ^ board.isWhitesTurn ^ flip)) continue;
					if (!mustBeSafe) return true;
					board.squares[x0, y0] = '-';
					if (!board.IsControlled(x + y0 * 8, !flip))
					{
						board.squares[x0, y0] = thisPiece;
						return true;
					}
					board.squares[x0, y0] = thisPiece;
				}
				for (int y = y0 - 1; y <= y0 + 1; y += 2)
				{
					if (y < 0 || y >= 8) continue;
					p = board.squares[x0, y];
					if (p != '-' && !(char.IsUpper(p) ^ board.isWhitesTurn ^ flip)) continue;
					if (!mustBeSafe) return true;
					board.squares[x0, y0] = '-';
					if (!board.IsControlled(x0 + y * 8, !flip))
					{
						board.squares[x0, y0] = thisPiece;
						return true;
					}
					board.squares[x0, y0] = thisPiece;
				}
				return false;
			}

			private static bool HasDiagonalValidMoves(int origin, Board board, bool flip, bool mustBeSafe = false)
			{
				int x0 = origin % 8;
				int y0 = origin / 8;
				char p;
				char thisPiece = board.squares[x0, y0];
				for (int x = x0 - 1; x <= x0 + 1; x += 2)
				{
					if (x < 0 || x >= 8) continue;
					for (int y = y0 - 1; y <= y0 + 1; y += 2)
					{
						if (y < 0 || y >= 8) continue;
						p = board.squares[x, y];
						if (p != '-' && !(char.IsUpper(p) ^ board.isWhitesTurn ^ flip)) continue;
						if (!mustBeSafe)
							return true;
						board.squares[x0, y0] = '-';
						if (!board.IsControlled(x + y * 8, !flip))
						{
							board.squares[x0, y0] = thisPiece;
							return true;
						}
						board.squares[x0, y0] = thisPiece;
					}
				}
				return false;
			}

			public static readonly Piece[] pieces = new Piece[] { Rook, Knight, Bishop, King, Queen, Pawn };
			public static char[] PieceCharacters
			{
				get
				{
					char[] result = new char[pieces.Length];
					for (int i = 0; i < result.Length; i++)
					{
						result[i] = pieces[i].representation;
					}
					return result;
				}
			}

			public static Piece FromRepresentation(char c)
			{
				foreach (Piece p in pieces)
				{
					if (p.representation == char.ToUpper(c)) return p;
				}
				return null;
			}
		}

		/// <summary>
		/// Represents a move in chess
		/// </summary>

		private class Move
		{
			public int origin;
			public int target;
			public bool isCastle;
			public bool isCapture;
			public bool isEnPassant;
			public bool isCheck;
			public bool isCheckMate;
			public char promote;

			public bool IsLegal(Board boardOriginal, out string error)
			{
				Board board = (Board)boardOriginal.Clone();
				board.ExecuteMove(this);
				if (!isCastle)
				{
					error = "King will be under attack - invalid move!";
					int kingPosition = board.isWhitesTurn ? board.blackKing : board.whiteKing;
					if (board.IsThreatened(kingPosition)) return false;
				}
				else
				{
					error = "King or rook will be under attack - invalid castle!";
					bool targetReached = false;
					for (int moveSquare = origin; !targetReached; moveSquare += (target - origin) > 0 ? 1 : -1)
					{
						if (board.IsThreatened(moveSquare)) return false;
						if (moveSquare == target) targetReached = true;
					}
				}
				error = "";
				return true;
			}

			public static bool TryParseMock(string input, Board board, out Move move, out string error)
			{
				move = new Move(-1, -1);
				error = "";
				if (Regex.IsMatch(input.ToUpper(), @"^[O0]\-[O0]([+#!?.\s]|$)"))
				{
					move.origin = !board.isWhitesTurn ? 60 : 4;
					move.target = !board.isWhitesTurn ? 62 : 6;
					move.isCastle = true;
					return true;
				}
				else if (Regex.IsMatch(input.ToUpper(), @"^[O0]\-[O0]\-[O0]([+#!?.\s]|$)"))
				{
					move.origin = !board.isWhitesTurn ? 60 : 4;
					move.target = !board.isWhitesTurn ? 58 : 2;
					move.isCastle = true;
					return true;
				}
				else if (input.Length != 4 && (input.Length != 5 || input[^1] != 'x') && (input.Length != 6 || input[^2..] != "xp")) { error = "wrong format"; return false; }

				if (!TryParseSquare(input[..2], out move.origin))
				{
					error = "wrong format on origin"; return false;
				}
				if (!TryParseSquare(input[2..4], out move.target))
				{
					error = "wrong format on target"; return false;
				}
				if (input[^1] == 'x') move.isCapture = true;
				if (input[^2..] == "xp")
				{
					move.isCapture = true;
					move.isEnPassant = true;
				}

				return true;
			}

			public static bool TryParse(string input, Board board, out Move move, out string error)
			{
				move = new(-1, -1);
				error = "";

				Match promotionSegment = Regex.Match(input, @"=[A-Z]([+#!?.]|$)");
				if (promotionSegment.Success)
				{
					move.promote = promotionSegment.Value[1];
					if (!Piece.PieceCharacters.Contains(move.promote))
					{
						error = $"\"{move.promote}\" cannot be parsed to a valid piece!";
						return true;
					}
					if (move.promote == Piece.King.representation || move.promote == Piece.Pawn.representation)
					{
						error = $"You can't promote a piece to a King or a Pawn.";
						return true;
					}
				}

				if (Regex.IsMatch(input.ToUpper(), @"^[O0]\-[O0]([+#!?.\s]|$)"))
				{
					if (!board.castling[board.isWhitesTurn ? 0 : 2])
					{
						error = "Short castling is currently unavailable!";
						return true;
					}
					move.origin = board.isWhitesTurn ? 60 : 4;
					move.target = board.isWhitesTurn ? 62 : 6;
					move.isCastle = true;
					int dir = move.target - move.origin > 0 ? 1 : -1;
					int rook = board.isWhitesTurn ? 63 : 7;
					for (int pos = move.origin + dir; pos != rook; pos += dir)
					{
						if (board.GetSquare(pos) != '-')
						{
							error = $"Can't castle king-side! A piece is obstructing the castle in {ToSquareName(ToMatrixCoords(pos))}.";
							return true;
						}
					}
					return true;
				}
				else if (Regex.IsMatch(input.ToUpper(), @"^[O0]\-[O0]\-[O0]([+#!?.\s]|$)"))
				{
					if (!board.castling[board.isWhitesTurn ? 1 : 3])
					{
						error = "Long castling is currently unavailable!";
						return true;
					}
					move.origin = board.isWhitesTurn ? 60 : 4;
					move.target = board.isWhitesTurn ? 58 : 2;
					move.isCastle = true;
					int dir = move.target - move.origin > 0 ? 1 : -1;
					int rook = board.isWhitesTurn ? 56 : 0;
					for (int pos = move.origin + dir; pos != rook; pos += dir)
					{
						if (board.GetSquare(pos) != '-')
						{
							error = $"Can't castle queen-side! A piece is obstructing the castle in {ToSquareName(ToMatrixCoords(pos))}.";
							return true;
						}
					}
					return true;
				}

				List<int> potentialOrigins = new();
				int rankFilter = -1;
				int fileFilter = -1;
				Piece toMove;
				Match explicitFormMatch = Regex.Match(input, @"^[a-h][1-8][x\s]*[a-hA-H][1-8]");

				if (explicitFormMatch.Success)
				{
					if (!TryParseSquare(explicitFormMatch.Value[..2], out move.origin))
					{
						error = $"The specified origin square ({explicitFormMatch.Value[..2]}) is invalid!";
						return true;
					}
					potentialOrigins.Add(move.origin);
					if (!Piece.PieceCharacters.Contains(char.ToUpper(board.GetSquare(move.origin))))
					{
						error = $"The specified origin square ({explicitFormMatch.Value[..2]}) doesn't contain a valid piece";
						return true;
					}
					toMove = Piece.FromRepresentation(board.GetSquare(move.origin));

					if (!TryParseSquare(explicitFormMatch.Value[^2..].ToLower(), out move.target))
					{
						error = $"The specified target square ({explicitFormMatch.Value[^2..]}) is invalid!";
						return true;
					}
					if (!toMove.isValid(move.origin, move.target, board, false))
					{
						error = "The targeted piece cannot move to the desired square!";
						return true;
					}
				}
				else
				{
					Match basicFormMatch = Regex.Match(input, @"^[A-Z]?[a-h1-8]?x?[a-hA-H][1-8]");

					if (basicFormMatch.Success)
					{
						string basicForm = basicFormMatch.Value;

						if (!TryParseSquare(basicForm[^2..].ToLower(), out move.target))
						{
							error = $"The specified target square ({basicForm[^2..]}) is invalid!";
							return true;
						}

						if (char.IsLower(basicForm[0])) toMove = Piece.Pawn;
						else if (!Piece.PieceCharacters.Contains(input[0]))
						{
							error = $"\"{input[0]}\" cannot be parsed to a valid piece!";
							return true;
						}
						else toMove = Piece.FromRepresentation(input[0]);

						if (Regex.IsMatch(basicForm, @"^[A-Z]?[a-h]x?[a-hA-H][1-8]"))
						{
							fileFilter = (char.IsLower(basicForm[0]) ? basicForm[0] : basicForm[1]) - 'a';
						}
						else if (Regex.IsMatch(basicForm, @"^[A-Z][1-8]x?[a-hA-H][1-8]"))
						{
							rankFilter = '8' - basicForm[1];
						}

						char targetPiece = board.isWhitesTurn ? toMove.representation : char.ToLower(toMove.representation);

						for (int x = 0; x < 8; x++)
						{
							if (fileFilter != -1 && fileFilter != x) continue;
							for (int y = 0; y < 8; y++)
							{
								if (rankFilter != -1 && rankFilter != y) continue;
								if (board.squares[x, y] == targetPiece) potentialOrigins.Add(x + y * 8);
							}
						}

						List<int> validOrigins = new();
						foreach (int origin in potentialOrigins)
						{
							if (toMove.isValid(origin, move.target, board, false))
							{
								validOrigins.Add(origin);
							}
						}

						if (validOrigins.Count == 0)
						{
							error = $"Invalid move! Unable to find any {toMove.name}" +
								$"{(fileFilter == -1 ? "" : $" in file {(char)('a' + fileFilter)}")}" +
								$"{(rankFilter == -1 ? "" : $" in rank {(char)('8' - rankFilter)}")}" +
								$" which can move to {ToSquareName(ToMatrixCoords(move.target))}";
							return true;
						}
						else if (validOrigins.Count > 1)
						{
							string[] ambiguousSquares = new string[validOrigins.Count];
							for (int i = 0; i < ambiguousSquares.Length; i++)
							{
								ambiguousSquares[i] = ToSquareName(ToMatrixCoords(validOrigins[i]));
							}
							error = $"Ambiguous move! A {toMove.name} can move to that position from: {string.Join(", ", ambiguousSquares)}";
							return true;
						}

						move.origin = validOrigins[0];
					}
					else return false;
				}

				if (move.origin >= 0 && move.target >= 0)
				{
					if (((move.target / 8 == 0 && board.isWhitesTurn) || (move.target / 8 == 7 && !board.isWhitesTurn))
						&& (toMove == Piece.Pawn))
					{
						if (move.promote == ' ') move.promote = Piece.Queen.representation;
					}
					else
						move.promote = ' ';
				}

				if (move.target == board.enPassant && toMove == Piece.Pawn && board.GetSquare(move.target) == '-' && (move.target - move.origin) % 8 != 0) { move.isEnPassant = true; move.isCapture = true; }
				if (board.GetSquare(move.target) != '-') move.isCapture = true;

				return true;
			}

			public Move(int origin, int target, bool isCastle = false, bool isCapture = false, bool isEnPassant = false, bool isCheck = false, bool isCheckMate = false, char promote = ' ')
			{
				this.origin = origin;
				this.target = target;
				this.isCastle = isCastle;
				this.isCapture = isCapture;
				this.isEnPassant = isEnPassant;
				this.isCheck = isCheck;
				this.isCheckMate = isCheckMate;
				this.promote = promote;
			}

			public List<int> ToHighlight()
			{
				List<int> result = new();

				result.Add(origin);
				result.Add(target);

				if (isCastle)
				{
					if (target % 8 < 4)
					{
						result.Add(target + 1);
						result.Add(target - 2);
					}
					else
					{
						result.Add(target - 1);
						result.Add(target + 1);
					}
				}
				return result;
			}

			public List<int> ToEnPassant()
			{
				List<int> result = new();

				result.Add((origin / 8) * 8 + target % 8);

				return result;
			}

			public List<int> ToDanger(Board board)
			{
				List<int> result = new();
				if (isCheck || isCheckMate)
				{
					result.Add(board.isWhitesTurn ? board.whiteKing : board.blackKing);
				}
				return result;
			}

			/// <summary>
			/// Stringifies the move.
			/// </summary>
			/// <returns>A string representing the origin and endpoint of the move.</returns>

			public override string ToString()
			{
				if (isCastle)
				{
					return target > origin ? "O-O" : "O-O-O";
				}
				Tuple<int, int> originpos = ToMatrixCoords(origin);
				Tuple<int, int> finalpos = ToMatrixCoords(target);
				return $"{ToSquareName(originpos)}{ToSquareName(finalpos)}{(isCapture ? "x" : "")}{(isEnPassant ? "p" : "")}";
			}
		}

		private enum Outcome
		{
			Playing,
			Draw,
			FiftyMoveRule,
			InsufficientMaterial,
			Checkmate,
			Check
		}

		private class Board
		{
			public char[,] squares;
			public bool isWhitesTurn;
			public bool[] castling;
			public int enPassant;
			public int halfmoves;
			public int fullmoves;

			public int whiteKing = -1;
			public int blackKing = -1;

			public static bool TryParseBoard(string fen, out Board board, out string error)
			{
				error = "";
				board = new Board();
				string[] components = fen.Split(" ");

				if (components.Length < 6)
				{
					error = "Missing components! The syntax must be `positions turn castling enpassant halfmoves fullmoves`";
					return false;
				}

				string[] ranks = components[0].Split('/');

				if (ranks.Length != 8)
				{
					error = "Positions expression doesn't have the correct number of ranks separated by '/'";
					return false;
				}

				char[] pieceChars = Piece.PieceCharacters;

				board.squares = new char[8, 8];
				for (int i = 0; i < 8; i++)
				{
					int counter = 0;
					foreach (char c in ranks[i])
					{
						if (counter >= 8)
						{
							error = $"Rank {8 - i} contains more than 8 positions.";
							return false;
						}
						if (!int.TryParse(c.ToString(), out int n))
						{
							if (!pieceChars.Contains(char.ToUpper(c)))
							{
								error = $"Rank {8 - i} contains an invalid piece: \"{c}\"";
								return false;
							}
							if (c == 'K')
							{
								if (board.whiteKing >= 0)
								{
									error = $"This board contains more than one white king!";
									return false;
								}
								board.whiteKing = i * 8 + counter;
							}
							else if (c == 'k')
							{
								if (board.blackKing >= 0)
								{
									error = $"This board contains more than one black king!";
									return false;
								}
								board.blackKing = i * 8 + counter;
							}
							board.squares[counter++, i] = c;
							continue;
						}
						if (counter + n > 8)
						{
							error = $"Rank {8 - i} contains more than 8 positions.";
							return false;
						}
						for (int j = 0; j < n; j++)
						{
							board.squares[counter++, i] = '-';
						}
					}
				}

				if (board.whiteKing < 0 || board.blackKing < 0)
				{
					error = $"Both colors must at least have a king!";
					return false;
				}

				board.isWhitesTurn = components[1] == "w";

				board.castling = new bool[] { components[2].Contains('K'),
					components[2].Contains('Q'),
					components[2].Contains('k'),
					components[2].Contains('q')
				};

				if (board.squares[0, 0] != 'r') board.castling[3] = false;
				if (board.squares[4, 0] != 'k') { board.castling[2] = false; board.castling[3] = false; }
				if (board.squares[7, 0] != 'r') board.castling[2] = false;
				if (board.squares[0, 7] != 'R') board.castling[1] = false;
				if (board.squares[4, 7] != 'K') { board.castling[0] = false; board.castling[1] = false; }
				if (board.squares[7, 7] != 'R') board.castling[0] = false;

				if (components[3] == "-") board.enPassant = -1;
				else if (!TryParseSquare(components[3], out board.enPassant))
				{
					error = "Unable to parse en-passant square into a valid square (a1-h8)";
					return false;
				}

				if (!int.TryParse(components[4], out board.halfmoves))
				{
					error = "Unable to parse halfmoves into an integer.";
					return false;
				}

				if (!int.TryParse(components[5], out board.fullmoves))
				{
					error = "Unable to parse fullmoves into an integer.";
					return false;
				}

				return true;
			}

			public void ExecuteMove(Move move)
			{
				int x0 = move.origin % 8;
				int y0 = move.origin / 8;
				int xf = move.target % 8;
				int yf = move.target / 8;
				char representation = squares[x0, y0];
				squares[x0, y0] = '-';
				squares[xf, yf] = representation;

				if (move.isEnPassant) squares[xf, y0] = '-';
				if (move.isCastle)
				{
					squares[xf < 4 ? 0 : 7, yf] = '-';
					squares[(x0 + xf) / 2, yf] = 'r'.MatchCase(representation);
				}
				bool isPawn = char.ToUpper(representation) == Piece.Pawn.representation;
				if (isPawn && Math.Abs(yf - y0) == 2)
					enPassant = (move.target + move.origin) / 2;
				else
					enPassant = -1;
				if (isPawn && move.promote != ' ') squares[xf, yf] = move.promote.MatchCase(representation);

				if (char.ToUpper(representation) == Piece.King.representation)
				{
					if (char.IsUpper(representation)) whiteKing = move.target;
					else blackKing = move.target;
				}

				isWhitesTurn = !isWhitesTurn;
				if (isWhitesTurn) fullmoves++;
				if (isPawn || move.isCapture) halfmoves = 0;
				else halfmoves++;
			}

			public Outcome GetOutcome()
			{
				if (halfmoves >= 100) return Outcome.FiftyMoveRule;
				if (InsufficientMaterial()) return Outcome.InsufficientMaterial;

				bool check = IsThreatenedVerbose(isWhitesTurn ? whiteKing : blackKing, true, out bool doubleAttack, out int attacker);
				bool noMove = !HasLegalMoves(check, doubleAttack, attacker);

				if (check && noMove)
				{
					return Outcome.Checkmate;
				}
				else if (check)
				{
					return Outcome.Check;
				}
				else if (noMove)
				{
					return Outcome.Draw;
				}
				return Outcome.Playing;
			}

			public bool InsufficientMaterial()
			{
				Dictionary<char, int> pieceCounts = new();
				foreach (char p in Piece.PieceCharacters)
				{
					pieceCounts.Add(p, 0);
					pieceCounts.Add(char.ToLower(p), 0);
				}
				for (int x = 0; x < 8; x++)
				{
					for (int y = 0; y < 8; y++)
					{
						char c = squares[x, y];
						if (c == '-') continue;
						else pieceCounts[c]++;
					}
				}
				for (int i = 0; i <= 1; i++)
				{
					char color = i == 0 ? 'a' : 'A';
					if (pieceCounts[Piece.Queen.representation.MatchCase(color)] > 0) return false;
					if (pieceCounts[Piece.Rook.representation.MatchCase(color)] > 0) return false;
					if (pieceCounts[Piece.Pawn.representation.MatchCase(color)] > 0) return false;
					if (pieceCounts[Piece.Bishop.representation.MatchCase(color)] + pieceCounts[Piece.Knight.representation.MatchCase(color)] > 1) return false;
				}
				return true;
			}

			public bool HasLegalMoves(bool inCheck, bool doubleAttack, int attackerPos)
			{
				HashSet<int> pinned = new();
				int kingPos = isWhitesTurn ? whiteKing : blackKing;

				if (!inCheck)
				{
					for (int pos = 0; pos < 64; pos++)
					{
						if (pos == kingPos) continue;
						char piecechar = GetSquare(pos);
						if (piecechar == '-' || char.IsLower(piecechar) == isWhitesTurn) continue;
						if (IsPiecePinned(pos, kingPos)) { pinned.Add(pos); }
						else
						{
							Piece piece = Piece.FromRepresentation(piecechar);
							if (piece.hasValidMoves(pos, this, false))
							{
								return true;
							}
						}
					}
					int kx = kingPos % 8;
					int ky = kingPos / 8;
					foreach (int pos in pinned)
					{
						char piecechar = GetSquare(pos);
						Piece piece = Piece.FromRepresentation(piecechar);

						int x0 = pos % 8;
						int y0 = pos / 8;
						int dx = Math.Sign(kx - x0);
						int dy = Math.Sign(ky - y0);
						for (int sign = -1; sign <= 1; sign += 2)
						{
							if (piece.isValid(pos, pos + dx * sign + dy * sign * 8, this, false)) return true;
						}
					}
					if (Piece.King.hasValidMoves(kingPos, this, false)) { return true; }
				}
				else
				{
					Piece attacker = Piece.FromRepresentation(GetSquare(attackerPos));
					HashSet<int> blockSquares = new();

					if (attacker.canPin && !doubleAttack)
					{
						int px = attackerPos % 8;
						int py = attackerPos / 8;
						int kx = kingPos % 8;
						int ky = kingPos / 8;

						int dx = px - kx;
						int dy = py - ky;

						int adv = Math.Sign(dx) + Math.Sign(dy) * 8;
						int newPos = kingPos + adv;
						while (newPos != attackerPos)
						{
							blockSquares.Add(newPos);
							newPos += adv;
						}
					}

					for (int pos = 0; pos < 64; pos++)
					{
						if (pos == kingPos) continue;
						char piecechar = GetSquare(pos);
						if (piecechar == '-' || char.IsLower(piecechar) == isWhitesTurn) continue;
						if (IsPiecePinned(pos, kingPos))
						{
							pinned.Add(pos);
						}
						else if (!doubleAttack)
						{ //If it is not pinned AND can capture only attacker, legal.
							Piece piece = Piece.FromRepresentation(piecechar);
							if (piece.isValid(pos, attackerPos, this, false)) return true;

							foreach (int sq in blockSquares)
							{ //If a piece can block the attack, legal.
								if (piece.isValid(pos, sq, this, false)) return true;
							}
						}
					}
					//If the king can move, legal.
					if (Piece.King.hasValidMoves(kingPos, this, false)) return true;
				}
				return false;
			}

			public char GetSquare(int value)
			{
				return squares[value % 8, value / 8];
			}

			public override string ToString()
			{
				StringBuilder builder = new(80);

				for (int y = 0; y < 8; y++)
				{
					if (y != 0) builder.Append('/');
					int spaces = 0;
					for (int x = 0; x < 8; x++)
					{
						if (squares[x, y] == '-') { spaces++; continue; }
						if (spaces > 0) { builder.Append(spaces); spaces = 0; }
						builder.Append(squares[x, y]);
					}
					if (spaces > 0) builder.Append(spaces);
				}

				builder.Append($" {(isWhitesTurn ? 'w' : 'b')} ");

				if (!castling[0] && !castling[1] && !castling[2] && !castling[3]) builder.Append('-');
				else
				{
					if (castling[0]) builder.Append('K');
					if (castling[1]) builder.Append('Q');
					if (castling[2]) builder.Append('k');
					if (castling[3]) builder.Append('q');
				}
				builder.Append(' ');

				string enPassantExpression = "-";
				if (enPassant >= 0)
				{
					enPassantExpression = ToSquareName(ToMatrixCoords(enPassant));
				}

				builder.Append($"{enPassantExpression} {halfmoves} {fullmoves}");

				return builder.ToString();
			}

			public object Clone()
			{
				Board output = new();

				output.squares = new char[8, 8];
				for (int i = 0; i < 64; i++)
				{
					output.squares.SetValue((char)squares[i / 8, i % 8], new int[] { i / 8, i % 8 });
				}
				output.isWhitesTurn = (bool)isWhitesTurn;
				output.castling = new bool[4];
				for (int i = 0; i < 4; i++)
				{
					output.castling.SetValue((bool)castling[i], i);
				}
				output.enPassant = (int)enPassant;
				output.halfmoves = (int)halfmoves;
				output.fullmoves = (int)fullmoves;
				output.whiteKing = (int)whiteKing;
				output.blackKing = (int)blackKing;

				return output;
			}

			public bool IsThreatened(int square, bool flipThreat = false)
			{
				for (int position = 0; position < 64; position++)
				{
					char pieceName = squares[position % 8, position / 8];
					if (!char.IsLetter(pieceName)) continue;
					if ((char.IsUpper(pieceName) == isWhitesTurn) ^ flipThreat)
					{
						Piece attacker = Piece.FromRepresentation(pieceName);
						if (attacker.isValid(position, square, this, flipThreat)) return true;
					}
				}
				return false;
			}

			public bool IsControlled(int square, bool flipThreat = false)
			{
				for (int position = 0; position < 64; position++)
				{
					char pieceName = squares[position % 8, position / 8];
					if (!char.IsLetter(pieceName)) continue;
					if ((char.IsUpper(pieceName) == isWhitesTurn) ^ flipThreat)
					{
						Piece attacker = Piece.FromRepresentation(pieceName);
						char temp = squares[square % 8, square / 8];
						char enemyPiece = isWhitesTurn ^ flipThreat ? char.ToLower(Piece.Pawn.representation) : char.ToUpper(Piece.Pawn.representation);
						squares[square % 8, square / 8] = enemyPiece;
						if (attacker.isValid(position, square, this, flipThreat))
						{
							squares[square % 8, square / 8] = temp;
							return true;
						}
						else
						{
							squares[square % 8, square / 8] = temp;
						}
					}
				}
				return false;
			}

			public bool IsThreatenedVerbose(int square, bool flipThreat, out bool multipleAttackers, out int firstAttacker)
			{
				multipleAttackers = false;
				firstAttacker = -1;
				for (int position = 0; position < 64; position++)
				{
					char pieceName = squares[position % 8, position / 8];
					if (!char.IsLetter(pieceName)) continue;
					if ((char.IsUpper(pieceName) == isWhitesTurn) ^ flipThreat)
					{
						Piece attacker = Piece.FromRepresentation(pieceName);
						if (attacker.isValid(position, square, this, flipThreat))
						{
							if (firstAttacker >= 0)
							{
								multipleAttackers = true;
								return true;
							}
							else
							{
								firstAttacker = position;
							}
						}
					}
				}
				return firstAttacker >= 0;
			}

			public bool IsPiecePinned(int piecePosition, int kingLocation)
			{
				if (piecePosition == kingLocation) return false;
				int px = piecePosition % 8;
				int py = piecePosition / 8;
				int kx = kingLocation % 8;
				int ky = kingLocation / 8;

				int dx = px - kx;
				int dy = py - ky;

				if (dx != 0 && dy != 0 && dx != dy && dx != -dy) return false;

				int xAdv = Math.Sign(dx);
				int yAdv = Math.Sign(dy);
				int x = kx + xAdv;
				int y = ky + yAdv;
				bool beforePiece = true;
				while (x >= 0 && x < 8 && y >= 0 && y < 8)
				{
					if (x == px && y == py) { beforePiece = false; x += xAdv; y += yAdv; continue; }
					if (squares[x, y] != '-')
					{
						if (beforePiece) return false;
						Piece p = Piece.FromRepresentation(squares[x, y]);
						return p.canPin && p.isValid(x + y * 8, piecePosition, this, true);
					}
					x += xAdv;
					y += yAdv;
				}
				return false;
			}

		}
	}
}
