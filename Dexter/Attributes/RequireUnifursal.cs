using System;
using Dexter.Enums;

namespace Dexter.Attributes.Methods
{

    /// <summary>
    /// The Require Unifursal attribute checks to see if a user has the Divine Fur role.
    /// If they have the role, they are sactioned to run the command. Else, the commands errors
    /// out to the user that they do not have the required permissions to run the set command.
    /// </summary>

    [AttributeUsage(AttributeTargets.Method)]

    public sealed class RequireUnifursalAttribute : RequirePermissionLevelAttribute
    {

        /// <summary>
        /// The constructor for the class, extending upon base the permission
        /// to be checked to be the unifursal permission.
        /// </summary>

        public RequireUnifursalAttribute() : base(PermissionLevel.Unifursal) { }

    }

}
