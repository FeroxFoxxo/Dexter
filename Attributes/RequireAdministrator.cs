using Dexter.Enums;
using System;

namespace Dexter.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireAdministratorAttribute : RequirePermissionLevelAttribute {
        public RequireAdministratorAttribute() : base(PermissionLevel.Administrator) { }
    }
}
