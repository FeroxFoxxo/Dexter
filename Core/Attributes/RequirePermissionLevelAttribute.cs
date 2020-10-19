using Dexter.Core.Abstractions;
using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Core.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public class RequirePermissionLevelAttribute : PreconditionAttribute {

        private readonly PermissionLevel Level;

        public RequirePermissionLevelAttribute(PermissionLevel _Level) {
            Level = _Level;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services) {
            return Task.FromResult((Context.User as IGuildUser).GetPermissionLevel((Context as CommandModule).BotConfiguration) >= Level
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"Haiya! It seems like you don't have the {Level} role! Are you sure you're a {Level.ToString().ToLower()}? <3"));
        }

    }
}
