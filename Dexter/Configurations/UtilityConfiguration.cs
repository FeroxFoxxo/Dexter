using System.Collections.Generic;
using Dexter.Abstractions;

namespace Dexter.Configurations
{

	/// <summary>
	/// Configures the relevant aspects of the Utility Commands Module.
	/// </summary>

	public class UtilityConfiguration : JSONConfig
	{

		/// <summary>
		/// The maximum number of items that can appear on an Embed Menu's page for upcoming reminders.
		/// </summary>

		public int ReminderMaxItemsPerPage { get; set; }

		/// <summary>
		/// The maximum length of a reminder that items in an embedMenu will appear with.
		/// </summary>

		public int ReminderMaxCharactersPerItem { get; set; }

		/// <summary>
		/// The prefix for all role colors.
		/// </summary>

		public string ColorRolePrefix { get; set; }

		/// <summary>
		/// Indicates which color roles are locked behind other roles.
		/// </summary>

		public Dictionary<ulong, ulong> LockedColors { get; set; }

		/// <summary>
		/// The width of each column in the color list menu.
		/// </summary>

		public int ColorListColWidth { get; set; }

		/// <summary>
		/// The amount of columns to display in the colors list.
		/// </summary>

		public int ColorListColCount { get; set; }

		/// <summary>
		/// The height of each row in the colors list.
		/// </summary>

		public int ColorListRowHeight { get; set; }

		/// <summary>
		/// Whether to display the colors listed in order by rows or by columns.
		/// </summary>

		public bool ColorListDisplayByRows { get; set; }

		/// <summary>
		/// The font size to display each color role name in.
		/// </summary>

		public int ColorListFontSize { get; set; }

		/// <summary>
		/// The set of role IDs for roles which grant a given permission level to change roles. "0" means no perms (default), "1" means only ranked color roles. "2" means all roles.
		/// </summary>

		public Dictionary<ulong, int> ColorChangeRoles { get; set; }

		/// <summary>
		/// The snowflake ID for the private voice chat category.
		/// </summary>
		
		public ulong PrivateCategoryID { get; set; }


		/// <summary>
		/// The name of the waiting VC that people will be in when waiting to be dragged in and out of private VCs.
		/// </summary>

		public string WaitingVCName { get; set; }

	}
}
