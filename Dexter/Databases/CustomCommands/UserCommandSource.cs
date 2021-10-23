namespace Dexter.Databases.CustomCommands
{
	/// <summary>
	/// Represents the type of user that is tied to the command
	/// </summary>
	public enum UserCommandSource
	{
		/// <summary>
		/// A generic custom command type, detached from any specific user
		/// </summary>
		Unspecified,

		/// <summary>
		/// The attached user is a patreon supporter of at least the minimum tier for custom commands
		/// </summary>
		Patreon,

		/// <summary>
		/// The attached user is a staff member
		/// </summary>
		Staff,

		Server
	}
}
