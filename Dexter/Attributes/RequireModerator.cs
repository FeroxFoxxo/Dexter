using System;
using Dexter.Enums;

namespace Dexter.Attributes.Methods
{

	/// <summary>
	/// The Require Moderator attribute checks to see if a user has the Moderator permission.
	/// If they have the permission, they are sactioned to run the command. Else, the commands errors
	/// out to the user that they do not have the required permissions to run the set command.
	/// </summary>

	[AttributeUsage(AttributeTargets.Method)]

	public sealed class RequireModeratorAttribute : RequirePermissionLevelAttribute
	{

		/// <summary>
		/// The constructor for the class, extending upon base the permission
		/// to be checked to be the moderator permission.
		/// </summary>

		public RequireModeratorAttribute() : base(PermissionLevel.Moderator) { }

	}

}
