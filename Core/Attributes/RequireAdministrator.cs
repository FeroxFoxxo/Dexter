using Dexter.Core.Enums;
using System;

namespace Dexter.Core.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireAdministratorAttribute : RequirePermissionLevelAttribute {
        public RequireAdministratorAttribute() : base(PermissionLevel.Administrator) { }
    }
}
