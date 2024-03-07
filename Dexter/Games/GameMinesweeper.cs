﻿using System;
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
using Dexter.Abstractions;
using Dexter.Enums;
using Dexter.Helpers;

namespace Dexter.Games
{
	class GameMinesweeper(GameInstance game, BotConfiguration botConfiguration) : GameTemplate(game, botConfiguration, "????/????/????/????, XXXX/X55X/X55X/XXXX, 0, 4, 4, 12")
	{
		const int MaxWidth = 26;
		const int MaxHeight = 18;
		const int MinWidth = 4;
		const int MinHeight = 4;
		const float MaxMineRatio = 0.4f;

		private int MaxMines
		{
			get
			{
				return (int)(Width * Height * MaxMineRatio);
			}
		}

		private string StateRaw
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

		private string BoardRaw
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

		private int Height
		{
			get
			{
				return int.Parse(Game.Data.Split(", ")[3]);
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[3] = value.ToString();
				Game.Data = string.Join(", ", newValue);
			}
		}

		private int Width
		{
			get
			{
				return int.Parse(Game.Data.Split(", ")[4]);
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[4] = value.ToString();
				Game.Data = string.Join(", ", newValue);
			}
		}

		private int Mines
		{
			get
			{
				return int.Parse(Game.Data.Split(", ")[5]);
			}
			set
			{
				string[] newValue = Game.Data.Split(", ");
				newValue[5] = value.ToString();
				Game.Data = string.Join(", ", newValue);
			}
		}

		private char[,] State
		{
			get
			{
				char[,] result = new char[Height, Width];
				string[] raw = StateRaw.Split('/');
				for (int i = 0; i < result.GetLength(0); i++)
				{
					for (int j = 0; j < result.GetLength(1); j++)
                    {
                        result[i, j] = raw[i][j];
                    }
                }
				return result;
			}
			set
			{
				StringBuilder builder = new();
				int h = value.GetLength(0);
				int w = value.GetLength(1);
				for (int i = 0; i < h; i++)
				{
					for (int j = 0; j < w; j++)
                    {
                        builder.Append(value[i, j]);
                    }

                    if (i != h - 1)
                    {
                        builder.Append('/');
                    }
                }
				StateRaw = builder.ToString();
			}
		}

		private char[,] Board
		{
			get
			{
				char[,] result = new char[Height, Width];
				string[] raw = BoardRaw.Split('/');
				for (int i = 0; i < result.GetLength(0); i++)
				{
					for (int j = 0; j < result.GetLength(1); j++)
                    {
                        result[i, j] = raw[i][j];
                    }
                }
				return result;
			}
			set
			{
				StringBuilder builder = new();
				int h = value.GetLength(0);
				int w = value.GetLength(1);
				for (int i = 0; i < h; i++)
				{
					for (int j = 0; j < w; j++)
                    {
                        builder.Append(value[i, j]);
                    }

                    if (i != h - 1)
                    {
                        builder.Append('/');
                    }
                }
				BoardRaw = builder.ToString();
			}
		}

		internal class Cell(int row, int col)
        {
			public int row = row;
			public int col = col;

            public override string ToString()
            {
				return $"{(char)('A' + col)}{1 + row}";
            }
        }

		private static char[,] GenerateBoard(int height, int width, int mineCount, Random rnd, Cell removeMine = null)
		{
			char[,] board = new char[height, width];

			int[,] dangers = new int[height, width];
			int[] mines = new int[height * width];
			for (int i = 0; i < height * width; i++)
            {
                mines[i] = i;
            }

            mines = [.. mines.OrderBy(x => rnd.Next())];

			int totalMines = 0;
			foreach (int i in mines)
			{
				int x = i % width;
				int y = i / width;
				if (removeMine is not null)
                {
					if (removeMine.row >= y - 1 && removeMine.row <= y + 1
						&& removeMine.col >= x - 1 && removeMine.col <= x + 1)
                    {
                        continue;
                    }
                } 

				for (int dx = -1; dx <= 1; dx++)
				{
					if (x + dx < 0 || x + dx >= width)
                    {
                        continue;
                    }

                    for (int dy = -1; dy <= 1; dy++)
					{
						if (y + dy < 0 || y + dy >= height)
                        {
                            continue;
                        }

                        dangers[y + dy, x + dx]++;
					}
				}
				board[y, x] = 'X';

				if (++totalMines >= mineCount)
                {
                    break;
                }
            }

			for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
				{
					if (board[y, x] != 'X')
                    {
                        board[y, x] = dangers[y, x].ToString()[0];
                    }
                }
            }

            return board;
		}

		private static char[,] GenerateNewState(int height, int width)
		{
			char[,] state = new char[height, width];

			for (int i = 0; i < state.GetLength(0); i++)
			{
				for (int j = 0; j < state.GetLength(1); j++)
				{
					state[i, j] = '?';
				}
			}

			return state;
		}

		private static bool CheckWin(char[,] state, char[,] board)
		{
			for (int i = 0; i < state.GetLength(0); i++)
			{
				for (int j = 0; j < state.GetLength(1); j++)
				{
					if (state[i, j] is '?' or 'F' && board[i, j] != 'X')
                    {
                        return false;
                    }
                }
			}
			return true;
		}

		private static bool CheckNew(char[,] state)
		{
			for (int i = 0; i < state.GetLength(0); i++)
			{
				for (int j = 0; j < state.GetLength(1); j++)
				{
					if (state[i, j] is not '?' and not 'F')
                    {
                        return false;
                    }
                }
			}

			return true;
		}

		private bool SmartProbe(out bool isLoss)
		{
			char[,] newState = State;
			isLoss = false;
			HashSet<Tuple<int, int>> toProbe = [];
			for (int x = 0; x < newState.GetLength(1); x++)
            {
                for (int y = 0; y < newState.GetLength(0); y++)
                {
                    if (newState[y, x] > '0' && newState[y, x] < '8')
					{
						foreach (Tuple<int, int> cell in SmartProbe(newState, x, y))
						{
							toProbe.Add(cell);
						}
					}
                }
            }

            char[,] board = Board;
			foreach (Tuple<int, int> cell in toProbe)
			{
				if (board[cell.Item2, cell.Item1] == 'X')
				{
					ProbeCell(cell.Item1, cell.Item2);
					isLoss = true;
					return true;
				}
				ProbeCellRecursive(ref newState, board, cell.Item1, cell.Item2);
			}

			if (toProbe.Count > 0)
			{
				State = newState;
				return true;
			}

			return false;
		}

		private static HashSet<Tuple<int, int>> SmartProbe(char[,] state, int x, int y)
		{
			int flags = 0;
			int goal = int.Parse(state[y, x].ToString());

			for (int dx = -1; dx <= 1; dx++)
			{
				if (x + dx < 0 || x + dx >= state.GetLength(1))
                {
                    continue;
                }

                for (int dy = -1; dy <= 1; dy++)
				{
					if (y + dy < 0 || y + dy >= state.GetLength(0))
                    {
                        continue;
                    }

                    if (state[y + dy, x + dx] == 'F')
                    {
                        flags++;
                    }
                }
			}

			HashSet<Tuple<int, int>> toProbe = [];
			if (flags == goal)
			{
				for (int dx = -1; dx <= 1; dx++)
				{
					if (x + dx < 0 || x + dx >= state.GetLength(1))
                    {
                        continue;
                    }

                    for (int dy = -1; dy <= 1; dy++)
					{
						if (y + dy < 0 || y + dy >= state.GetLength(0))
                        {
                            continue;
                        }

                        if (state[y + dy, x + dx] == '?')
                        {
                            toProbe.Add(new Tuple<int, int>(x + dx, y + dy));
                        }
                    }
				}
			}
			return toProbe;
		}

		private bool ProbeCell(int x, int y, ref char[,] state, char[,] board)
		{
			if (state[y, x] != '?')
            {
                return false;
            }

            state[y, x] = board[y, x];

			switch (state[y, x])
			{
				case 'X':
					for (int i = 0; i < state.GetLength(0); i++)
					{
						for (int j = 0; j < state.GetLength(1); j++)
						{
							if (board[i, j] == 'X')
                            {
                                state[i, j] = 'X';
                            }
                        }
					}
					break;
				case '0':
					for (int dx = -1; dx <= 1; dx++)
					{
						if (x + dx < 0 || x + dx >= state.GetLength(1))
                        {
                            continue;
                        }

                        for (int dy = -1; dy <= 1; dy++)
						{
							if (y + dy < 0 || y + dy >= state.GetLength(0))
                            {
                                continue;
                            }

                            ProbeCellRecursive(ref state, board, x + dx, y + dy);
						}
					}
					break;
			}
			return true;
		}

		private bool ProbeCell(int x, int y)
		{
			char[,] state = State;

			bool result = ProbeCell(x, y, ref state, Board);

			State = state;
			return result;
		}

		private bool ProbeCells(IEnumerable<Cell> cells, out bool isLoss)
		{
			char[,] state = State;
			bool result = false;
			isLoss = false;

			foreach (Cell cell in cells)
			{
				if (ProbeCell(cell.col, cell.row, ref state, Board))
				{
					result = true;
					if (state[cell.row, cell.col] == 'X')
                    {
                        isLoss = true;
                    }
                }
			}

			State = state;
			return result;
		}

		private void ProbeCellRecursive(ref char[,] state, char[,] board, int x, int y)
		{
			if (state[y, x] != '?')
            {
                return;
            }

            state[y, x] = board[y, x];

			if (board[y, x] == '0')
			{
				for (int dx = -1; dx <= 1; dx++)
				{
					for (int dy = -1; dy <= 1; dy++)
					{
						if (x + dx < 0 || x + dx >= state.GetLength(1))
                        {
                            continue;
                        }

                        if (y + dy < 0 || y + dy >= state.GetLength(0))
                        {
                            continue;
                        }

                        ProbeCellRecursive(ref state, board, x + dx, y + dy);
					}
				}
			}
		}

		const int cellSize = 32;
		private Bitmap RenderMatrixImage(char[,] matrix)
		{
			Bitmap result = new(cellSize * (matrix.GetLength(1) + 2), cellSize * (matrix.GetLength(0) + 2));

			Dictionary<char, System.Drawing.Image> cellImages = [];
			foreach (KeyValuePair<char, string> kvp in CellImageNames)
			{
				cellImages.Add(kvp.Key, System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, $"{kvp.Value}.png")));
			}

			using (Graphics g = Graphics.FromImage(result))
			{
				for (int x = 1; x < matrix.GetLength(1) + 1; x++)
				{
					for (int y = 1; y < matrix.GetLength(0) + 1; y++)
					{
						g.DrawImage(cellImages[matrix[y - 1, x - 1]], x * cellSize, y * cellSize, cellSize, cellSize);
					}
				}

				using (System.Drawing.Image corner = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, $"{Corner}.png")))
				{
					for (int x = 0; x < 2; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            g.DrawImage(corner, x * (result.Width - cellSize), y * (result.Height - cellSize), cellSize, cellSize);
                        }
                    }
                }

				System.Drawing.Image[] letterLabels = GetLabels(matrix.GetLength(1), false);
				for (int y = 0; y < 2; y++)
				{
					int ry = y * (result.Height - cellSize);
					using (System.Drawing.Image label = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, $"{LetterLabels[y]}.png")))
					{
						for (int x = cellSize; x < result.Width - cellSize; x += cellSize)
						{
							g.DrawImage(label, x, ry, cellSize, cellSize);
						}
					}

					for (int x = 1; x < letterLabels.Length + 1; x++)
					{
						g.DrawImage(letterLabels[x - 1], x * cellSize, ry, cellSize, cellSize);
					}
				}

				System.Drawing.Image[] numberLabels = GetLabels(matrix.GetLength(0), true);
				for (int x = 0; x < 2; x++)
				{
					int rx = x * (result.Width - cellSize);
					using (System.Drawing.Image label = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, $"{NumLabels[x]}.png")))
					{
						for (int y = cellSize; y < result.Height - cellSize; y += cellSize)
						{
							g.DrawImage(label, rx, y, cellSize, cellSize);
						}
					}

					for (int y = 1; y < numberLabels.Length + 1; y++)
					{
						g.DrawImage(numberLabels[y - 1], rx, y * cellSize, cellSize, cellSize);
					}
				}
			}

			return result;
		}

		private const string MinesweeperPath = "Images/Games/Minesweeper";
		private const string LabelsDirectory = "Labels";
		private const string Corner = "GridCorner";
		private readonly string[] NumLabels = ["NumberLabelLeft", "NumberLabelRight"];
		private readonly string[] LetterLabels = ["LetterLabelTop", "LetterLabelBottom"];
		private readonly Dictionary<char, string> CellImageNames = new() {
			{'0', "Cell0"},
			{'1', "Cell1"},
			{'2', "Cell2"},
			{'3', "Cell3"},
			{'4', "Cell4"},
			{'5', "Cell5"},
			{'6', "Cell6"},
			{'7', "Cell7"},
			{'8', "Cell8"},
			{'?', "Cell"},
			{'X', "Mine"},
			{'F', "CellFlag"}
		};

		private static System.Drawing.Image[] GetLabels(int length, bool isNumber)
		{
			System.Drawing.Image[] result = new System.Drawing.Image[length];

			for (int i = 0; i < length; i++)
			{
				if (isNumber)
                {
                    result[i] = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, LabelsDirectory, $"{i + 1}.png"));
                }
                else
                {
                    result[i] = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, LabelsDirectory, $"{(char)('A' + i)}.png"));
                }
            }

			return result;
		}

		public override EmbedBuilder GetStatus(DiscordShardedClient client)
		{
			return BuildEmbed(EmojiEnum.Unknown)
				.WithColor(Discord.Color.Blue)
				.WithTitle($"{Game.Title} (Game {Game.GameID})")
				.WithDescription($"{Game.Description}")
				.AddField("Dimensions", $"{Width}Ã—{Height}", true)
				.AddField("Mines", $"{Mines}ðŸ’£", true)
				.AddField("Master", client.GetUser(Game.Master)?.GetUserInformation() ?? "<N/A>")
				.AddField(Game.Banned.Length > 0, "Banned Players", Game.BannedMentions.TruncateTo(500));
		}

		public override void Reset(FunConfiguration funConfiguration, GamesDB gamesDB)
		{
			Game.Data = EmptyData;
			Game.LastUserInteracted = Game.Master;
			if (gamesDB is null)
            {
                return;
            }

            Player[] players = gamesDB.GetPlayersFromInstance(Game.GameID);
			foreach (Player p in players)
			{
				p.Score = 0;
				p.Lives = 0;
			}
		}

		public override bool Set(string field, string value, FunConfiguration funConfiguration, out string feedback)
		{
			bool nonNumeric = false;
			string nonNumericFeedback = $"This field is numeric, unable to parse \"{value}\" into an integer value.\n" +
					$"Did you mean to use a default field instead?";
			if (!int.TryParse(value, out int number))
            {
                nonNumeric = true;
            }

            int mines = Mines;

			switch (field.ToLower())
			{
				case "width":
					if (nonNumeric) { feedback = nonNumericFeedback; return false; }
					if (number > MaxWidth || number < MinWidth)
					{
						feedback = $"Invalid width! The width must be between {MinWidth} and {MaxWidth}.";
						return false;
					}
					Width = number;
					if (mines > MaxMines)
                    {
                        mines = MaxMines;
                    }

                    Board = GenerateBoard(Height, Width, mines, new Random());
					State = GenerateNewState(Height, Width);
					feedback = $"Set \"width\" to {number} and regenerated game board [{Width}x{Height}]. Maximum mine count for this size is {MaxMines}.";
					return true;
				case "height":
					if (nonNumeric) { feedback = nonNumericFeedback; return false; }
					if (number > MaxHeight || number < MinHeight)
					{
						feedback = $"Invalid height! The height must be between {MinHeight} and {MaxHeight}.";
						return false;
					}
					Height = number;
					if (mines > MaxMines)
                    {
                        mines = MaxMines;
                    }

                    Board = GenerateBoard(Height, Width, mines, new Random());
					State = GenerateNewState(Height, Width);
					feedback = $"Set \"height\" to {number} and regenerated game board [{Width}x{Height}]. Maximum mine count for this size is {MaxMines}.";
					return true;
				case "mines":
					if (nonNumeric) { feedback = nonNumericFeedback; return false; }
					if (number < 0)
					{
						feedback = $"Invalid value! Number of mines can't be a negative number.";
						return false;
					}
					feedback = $"Set \"mines\" to {number} and regenerated game board [{Width}x{Height}]. Maximum mine count for this size is {MaxMines}.";
					Mines = mines = number;
					if (mines > MaxMines)
                    {
                        mines = MaxMines;
                    }

                    Board = GenerateBoard(Height, Width, mines, new Random());
					State = GenerateNewState(Height, Width);
					return true;
				case "size":
					string[] toParse = value.Split(" ");
					List<int> numbers = [];
					foreach (string s in toParse)
					{
						if (int.TryParse(s, out int n))
                        {
                            numbers.Add(n);
                        }
                    }
					if (numbers.Count < 2)
					{
						feedback = $"You didn't provide enough numbers, please use the field-value syntax `size [WIDTH] [HEIGHT] (MINES)`.";
						return false;
					}
					if (numbers[0] > MaxWidth || numbers[0] < MinWidth)
					{
						feedback = $"Invalid width! The width must be between {MinWidth} and {MaxWidth}.";
						return false;
					}
					if (numbers[1] > MaxHeight || numbers[1] < MinHeight)
					{
						feedback = $"Invalid height! The height must be between {MinHeight} and {MaxHeight}.";
						return false;
					}

					Width = numbers[0];
					Height = numbers[1];
					if (numbers.Count > 2)
                    {
                        Mines = mines = numbers[2];
                    }
                    else
                    {
                        mines = Mines;
                    }

                    if (mines > MaxMines)
                    {
                        mines = MaxMines;
                    }

                    feedback = $"Set board size to [{Width}x{Height}] with a maximum mine count of {Mines}, current size can hold up to {MaxMines} mines.";
					Board = GenerateBoard(Height, Width, mines, new Random());
					State = GenerateNewState(Height, Width);
					return true;
				case "difficulty":
					value = value.ToLower();
					if (!Difficulties.TryGetValue(value, out string value))
					{
						feedback = $"\"{value}\" is not a valid difficulty! Available difficulties are: {string.Join(", ", Difficulties.Keys)}";
						return false;
					}
					Set("size", value, funConfiguration, out feedback);
					return true;
			}

			feedback = $"Invalid field: \"{field}\" is not a default field nor \"width\", \"height\", \"mines\", \"size\", or \"difficulty\".";
			return false;
		}

		public override EmbedBuilder Info(FunConfiguration funConfiguration)
		{
			return BuildEmbed(EmojiEnum.Unknown)
				.WithColor(Discord.Color.Magenta)
				.WithTitle("How to Play: Minesweeper")
				.WithDescription("**Step 1**: Set your desired parameters for the board using the `~game set [field] [value]` command. These fields are `width`, `height` and `mines`.\n" +
					"Here are some defaults, if you'd like to try them:\n" +
					"Beginner: [10x10] (10ðŸ’£)\n" +
					"Intermediate: [16x16] (40ðŸ’£)\n" +
					"Advanced: [26x18] (99ðŸ’£)\n" +
					"**Step 2**: Create the board view by typing `board` into the game chat.\n" +
					"**Step 3**: Begin probing cells! Type the name of one or more cells (e.g. `C3` or `G12 D4 H3`) to probe them. Or type `flag [Cell] (Cells...)` to toggle a flag in one or more cells.)\n" +
					"Since this game is singleplayer, only the game master can interact with it. The number on each cell tells you how many mines are on its immediate surroundings, anywhere from 0 to 8. If you probe a mine, it's game over!\n" +
					"You win once there are no more cells to probe that do not contain a mine.\n" +
					"PRO TIP! Type `auto` to automatically probe any cells deemed safe by surrounding flags and danger levels.");
		}

		readonly Dictionary<string, string> Difficulties = new()
		{
			{"beginner", "10 10 10"},
			{"intermediate", "16 16 40"},
			{"advanced", "26 18 99"}
		};

        private bool TryParsePos(string input, out Cell result)
		{
			result = new Cell(0, 0);
			if (input.Length < 2)
            {
                return false;
            }

            input = input.ToUpper();

			if (input[0] < 'A' || input[0] > 'Z')
            {
                return false;
            }

            int x = input[0] - 'A';

			if (!int.TryParse(input[1..], out int y))
            {
                return false;
            }

            result = new Cell(--y, x);
			return true;
		}

		private Cell ToRealPos(Cell input)
		{
			int y = input.row;
			if (y >= Height)
            {
                y = Height - 1;
            }

            if (y < 0)
            {
                y = 0;
            }

            return new Cell(y, input.col);
		}

		public override async Task HandleMessage(IMessage message, GamesDB gamesDB, DiscordShardedClient client, FunConfiguration funConfiguration)
		{
			bool deletePerms = message.Channel is not IDMChannel;
			if (message.Author.Id != Game.Master)
            {
                return;
            }

            string msg = message.Content.ToLower().Replace("@", "@-");
			string[] args = msg.Split(' ');

			IUserMessage board = null;
			if (BoardID != 0)
            {
                board = await message.Channel.GetMessageAsync(BoardID) as IUserMessage;
            }

            bool toRender = false;
			bool isLoss = false;
			if (msg is "board" or "render")
			{
				toRender = true;
			}

			if (msg == "solve")
			{
				if (board is not null)
                {
                    await board.DeleteAsync();
                }

                string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
				string filepath = Path.Join(imageChacheDir, $"Minesweeper{Game.Master}_SOLUTION.png");
				System.Drawing.Image image = RenderMatrixImage(Board);
				image.Save(filepath);
				IUserMessage newBoard = await message.Channel.SendFileAsync(filepath);
				BoardID = newBoard.Id;
				if (deletePerms)
                {
                    await message.DeleteAsync();
                }

                return;
			}

			if (msg == "reset")
			{
				BoardID = 0;
				Board = GenerateBoard(Height, Width, Mines < MaxMines ? Mines : MaxMines, new Random());
				State = GenerateNewState(Height, Width);
				await message.Channel.SendMessageAsync($"Reset your {Width} by {Height} board!");
				return;
			}

			if (msg == "auto")
			{
				if (!SmartProbe(out bool smartLoss))
				{
					await message.Channel.SendMessageAsync("Couldn't find any safe cells, are you missing flags?");
					return;
				}
				isLoss |= smartLoss;
				toRender = true;
			}

			if (msg.StartsWith("flag") || msg.StartsWith("unflag") || msg.StartsWith("f "))
			{
				if (args.Length < 2)
				{
					await message.Channel.SendMessageAsync("You must provide a position to flag or unflag!");
					return;
				}
				List<Cell> toFlag = [];
				for (int i = 1; i < args.Length; i++)
				{
					if (!TryParsePos(args[i], out Cell flagpos))
					{
						await message.Channel.SendMessageAsync($"Unable to resolve position {args[1]}, please type a letter followed by a number, no spaces!");
						return;
					}
					toFlag.Add(ToRealPos(flagpos));
				}

				char[,] state = State;
				foreach (Cell cell in toFlag)
				{
					if (state[cell.row, cell.col] is not '?' and not 'F')
					{
						await message.Channel.SendMessageAsync($"Unable to toggle flag at ({cell}), it has already been probed!");
						return;
					}
					else
					{
						state[cell.row, cell.col] = state[cell.row, cell.col] == 'F' ? '?' : 'F';
					}
				}
				State = state;

				toRender = true;
			}

			string[] positions = msg.Split(" ");
			if (TryParsePos(positions[0], out _))
			{
				List<Cell> cells = [];
				foreach (string s in positions)
				{
					if (TryParsePos(s, out Cell newItem))
					{
						cells.Add(ToRealPos(newItem));
					}
				}

				if (cells.Count > 0)
				{
					char firstC = Board[cells[0].row, cells[0].col];
					if (firstC != '0')
					{
						if (CheckNew(State))
						{
							Random rnd = new();
							char[,] newBoard = GenerateBoard(Height, Width, Math.Min(Mines, MaxMines), rnd, cells[0]);
							Board = newBoard;
						}
						else if (firstC == 'X')
                        {
                            isLoss = true;
                        }
                    }

					if (!ProbeCells(cells, out bool probeLoss))
					{
						await message.Channel.SendMessageAsync($"All provided cells have already been probed or are flagged!");
						return;
					}
					isLoss |= probeLoss;
					toRender = true;
				}
			}

			if (toRender)
			{
				if (board is not null)
                {
                    await board.DeleteAsync();
                }

                string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
				string filepath = Path.Join(imageChacheDir, $"Minesweeper{Game.Master}.png");
				System.Drawing.Image image = RenderMatrixImage(State);
				image.Save(filepath);
				IUserMessage newBoard = await message.Channel.SendFileAsync(filepath);
				BoardID = newBoard.Id;
				if (deletePerms)
                {
                    await message.DeleteAsync();
                }
            }

			if (isLoss)
			{
				_ = Mines > MaxMines ? MaxMines : Mines;
				await BuildEmbed(EmojiEnum.Unknown)
					.WithColor(Discord.Color.Red)
					.WithTitle("Defeat!")
					.WithDescription($"Whoops! You probed a mine! Better luck next time.")
					.SendEmbed(message.Channel);
				BoardID = 0;
				Board = GenerateBoard(Height, Width, Mines < MaxMines ? Mines : MaxMines, new Random());
				State = GenerateNewState(Height, Width);
				return;
			}

			if (CheckWin(State, Board))
			{
				int mines = Mines > MaxMines ? MaxMines : Mines;
				await BuildEmbed(EmojiEnum.Unknown)
					.WithColor(Discord.Color.Green)
					.WithTitle("Victory!")
					.WithDescription($"Cleared the board! ({Width}x{Height}) with {mines} mine{(mines != 1 ? "s" : "")}!")
					.SendEmbed(message.Channel);
				BoardID = 0;
				Board = GenerateBoard(Height, Width, Mines < MaxMines ? Mines : MaxMines, new Random());
				State = GenerateNewState(Height, Width);
				return;
			}
		}
	}
}
