using System.Collections.Generic;
namespace Dexter.Databases.Games
{

	/// <summary>
	/// Has information about conversion from natural language to internal Enums.
	/// </summary>

	public static class GameTypeConversion
	{

		/// <summary>
		/// Converts a lowercase string without spaces into the appropriate game.
		/// </summary>

		public static readonly Dictionary<string, GameType> GameNames = new() {
			{"hangman", GameType.Hangman },
			{"tictactoe", GameType.TicTacToe},
			{"tic-tac-toe", GameType.TicTacToe },
			{"tic", GameType.TicTacToe },
			{"connect4", GameType.Connect4 },
			{"connectfour", GameType.Connect4 },
			{"minesweeper", GameType.Minesweeper },
			{"chess", GameType.Chess}
			//{"wordchain", GameType.Shiritori},
			//{"shiritori", GameType.Shiritori}
		};

		/// <summary>
		/// Correlates each GameType with an emoji for a very small representation.
		/// </summary>

		public static readonly Dictionary<GameType, string> GameEmoji = new() {
			{GameType.Unselected, "❓"},
			{GameType.Hangman, "💀"},
			{GameType.TicTacToe, "⭕"},
			{GameType.Connect4, "4️⃣" },
			{GameType.Minesweeper, "💣" },
			{GameType.Chess, "♟️"}
			//{GameType.Shiritori, "⛓"},
			//{GameType.Charades, "🎭"},
		};

	}
}
