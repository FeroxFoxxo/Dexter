using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Core {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireModerator : PreconditionAttribute {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services) {
            return Task.FromResult((Context.User as IGuildUser).RoleIds.ToList().Contains(12121)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError(Context.User.Mention + " does not have the Moderator role!"));
        }
    }
}
