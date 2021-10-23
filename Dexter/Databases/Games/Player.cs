using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Games
{

	/// <summary>
	/// Represents an individual player in the Games System database, and thus a user on Discord in general.
	/// </summary>

	public class Player
	{

		/// <summary>
		/// The unique user ID for the user that this player represents.
		/// </summary>

		[Key]
		public ulong UserID { get; set; }

		/// <summary>
		/// What game session the player is playing in.
		/// </summary>

		public int Playing { get; set; }

		/// <summary>
		/// This player's score in the game they're playing.
		/// </summary>

		public double Score { get; set; }

		/// <summary>
		/// This player's number of lives in the game they're playing.
		/// </summary>

		public int Lives { get; set; }

		/// <summary>
		/// Any additional data that can be used locally at a per-<see cref="Abstractions.GameTemplate"/> level.
		/// </summary>

		public string Data { get; set; }

	}
}
