using Dexter.Core.Enums;
using System;

namespace Dexter.Core.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireModeratorAttribute : RequirePermissionLevelAttribute {
        public RequireModeratorAttribute() : base(PermissionLevel.Moderator) { }
    }
}
