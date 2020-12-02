using Dexter.Enums;
using System;

namespace Dexter.Attributes {

    /// <summary>
    /// The Require Administrator attribute checks to see if a user has the Administrative permission.
    /// If they have the permission, they are sactioned to add the parameter. Else, the commands errors
    /// out to the user that they do not have the required permissions to add the set parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class RequireAdministratorParameterAttribute : RequirePermissionLevelParameterAttribute {

        /// <summary>
        /// The constructor for the class, extending upon base the permission
        /// to be checked to be the administrator permission.
        /// </summary>
        public RequireAdministratorParameterAttribute() : base(PermissionLevel.Administrator) { }

    }

}
