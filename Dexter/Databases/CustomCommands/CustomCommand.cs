using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.CustomCommands
{

	/// <summary>
	/// The CustomCommand class contains information on a custom command, including its name,
	/// the reply it gives, and its possible aliases of sorts.
	/// </summary>

	public class CustomCommand
	{

		/// <summary>
		/// The CommandName is the KEY of the table.
		/// It is the default name of the command.
		/// </summary>

		[Key]

		public string CommandName { get; set; }

		/// <summary>
		/// The Reply field is what the bot will respond with once the command has been run.
		/// </summary>

		public string Reply { get; set; }

		/// <summary>
		/// The Alias field is a string of possible aliases a command may have.
		/// Each alias is split by a comma (,).
		/// </summary>

		public string Alias { get; set; }

		/// <summary>
		/// The user attached to this command, 0 if none.
		/// </summary>
		public ulong User { get; set; }

		/// <summary>
		/// The type of user attached to the command.
		/// </summary>
		public UserCommandSource CommandType { get; set; }

	}

}
