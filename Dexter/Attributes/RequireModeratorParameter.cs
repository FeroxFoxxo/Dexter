using Dexter.Enums;
using System;

namespace Dexter.Attributes {

    /// <summary>
    /// The Require Moderator attribute checks to see if a user has the Moderator permission on a parameter.
    /// If they have the permission, they are sactioned to add the parameter. Else, the commands errors
    /// out to the user that they do not have the required permissions to add the set parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireModeratorParameterAttribute : RequirePermissionLevelParameterAttribute {

        /// <summary>
        /// The constructor for the class, extending upon base the permission
        /// to be checked to be the moderator permission.
        /// </summary>
        public RequireModeratorParameterAttribute() : base(PermissionLevel.Moderator) { }

    }

}
