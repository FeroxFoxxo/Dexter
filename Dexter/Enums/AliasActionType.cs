namespace Dexter.Enums
{

	/// <summary>
	/// An enum of the types of actions you can run through the ~ccalias command.
	/// </summary>

	public enum AliasActionType
	{

		/// <summary>
		/// The ADD value specifies the user is adding an alias to a custom command.
		/// </summary>

		Add,

		/// <summary>
		/// The REMOVE value specifies the user is trying to remove an alias from a custom command.
		/// </summary>

		Remove,

		/// <summary>
		/// The LIST value specifies that the user is attempting to query the command which has this alias.
		/// </summary>

		List

	}

}
