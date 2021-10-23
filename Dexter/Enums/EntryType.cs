namespace Dexter.Enums
{

	/// <summary>
	/// This enum is to be able to see whether or not this entry has been removed or not.
	/// If this entry has been removed, it will be set to the REMOVED state - otherwise,
	/// it will have the VALID value state.
	/// </summary>

	public enum EntryType
	{

		/// <summary>
		/// The issue enum state is to differenciate this entry to one that has been removed.
		/// When an entry is created, it is automatically set to this enum value unless a command
		/// has been run that has changed its state into a removed state.
		/// </summary>

		Issue,

		/// <summary>
		/// Whenever an entry is revoked, it is ommitted from its command.
		/// This is to help with accidental removals.
		/// </summary>

		Revoke

	}

}
