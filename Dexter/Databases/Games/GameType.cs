namespace Dexter.Databases.Games
{

	/// <summary>
	/// Indicates a game type that a game instance can take on.
	/// </summary>

	public enum GameType
	{

		/// <summary>
		/// The default GameType, it should largely be unused.
		/// </summary>

		Unselected,

		/// <summary>
		/// An instance game of Hangman, a game about guessing an obscured word by guessing individual letters.
		/// </summary>

		Hangman,

		/// <summary>
		/// An instance of a game of TicTacToe, a game about placing a token in a 3x3 board to make a line.
		/// </summary>

		TicTacToe,

		/// <summary>
		/// An instance of a game of Connect 4, a game about placing tokens on a 9x6 vertical, gravity-affected grid to make a line of 4.
		/// </summary>

		Connect4,

		/// <summary>
		/// An instance of a game of Minesweeper, a game about probing different cells in a board filled with mines without triggering them.
		/// </summary>

		Minesweeper,

		/// <summary>
		/// An instance of a game of Chess, a game about putting the enemy in checkmate while avoiding the same.
		/// </summary>

		Chess,

		/// <summary>
		/// An instance game of Shiritori, a game about coming up with words that start by whatever the previous person's word ends.
		/// </summary>

		//[NotImplemented]
		Shiritori,

		/// <summary>
		/// An instance game of Charades, a game about guessing what a person is trying to nonverbally communicate.
		/// </summary>

		//[NotImplemented]
		Charades
	}
}
