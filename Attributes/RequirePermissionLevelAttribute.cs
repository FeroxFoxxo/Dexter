using Dexter.Abstractions;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public class RequirePermissionLevelAttribute : PreconditionAttribute {

        private readonly PermissionLevel Level;

        public RequirePermissionLevelAttribute(PermissionLevel _Level) {
            Level = _Level;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services) {
            return Task.FromResult((Context.User as IGuildUser).GetPermissionLevel((Context as CommandModule).BotConfiguration) >= Level
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"User does not meet the permission level {Level}."));
        }

    }
}
