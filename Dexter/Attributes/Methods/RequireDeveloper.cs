using Dexter.Enums;
using System;

namespace Dexter.Attributes.Methods {

    /// <summary>
    /// The Require Developer attribute checks to see if a user has the Developer role.
    /// If they have the role, they are sactioned to run the command. Else, the commands errors
    /// out to the user that they do not have the required permissions to run the set command.
    /// </summary>
    
    [AttributeUsage(AttributeTargets.Method)]

    public sealed class RequireDeveloperAttribute : RequirePermissionLevelAttribute {

        /// <summary>
        /// The constructor for the class, extending upon base the permission
        /// to be checked to be the administrator permission.
        /// </summary>
        
        public RequireDeveloperAttribute() : base(PermissionLevel.Developer) { }

    }

}
