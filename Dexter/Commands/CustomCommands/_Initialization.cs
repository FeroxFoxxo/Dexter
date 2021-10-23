using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.CustomCommands;
using System.Linq;

namespace Dexter.Commands
{

	/// <summary>
	/// The CustomCommands module relates to the addition, removal, editing and listing of custom commands.
	/// </summary>

	public partial class CustomCommands : DiscordModule
	{

		/// <summary>
		/// The CustomCommandDB contains all the custom commands that has been added to the bot.
		/// </summary>
		public CustomCommandDB CustomCommandDB { get; set; }

		/// <summary>
		/// The relevant configuration required to manage and interact with custom commands.
		/// </summary>

		public CustomCommandsConfiguration CustomCommandsConfiguration { get; set; }

		/// <summary>
		/// Obtains a user command by the user attached to it.
		/// </summary>
		/// <param name="id">The id of the target user.</param>
		/// <param name="type">The type of command to search for, any if unspecified.</param>
		/// <returns><see langword="null"/> if no command exists attached to that user; otherwise returns the respective <see cref="CustomCommand"/>.</returns>
		public CustomCommand GetCommandByUser(ulong id, UserCommandSource type = UserCommandSource.Unspecified)
		{
			if (id == 0) return null;
			return CustomCommandDB.CustomCommands.AsQueryable().Where(uc => uc.User == id && (type == UserCommandSource.Unspecified || uc.CommandType == type)).FirstOrDefault();
		}

	}

}
