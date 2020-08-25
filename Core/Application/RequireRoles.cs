using Dexter.Core.Abstractions;
using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Core.DiscordApp {
    public sealed class RequireModerator : PreconditionAttribute {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services) {
            IGuildUser User = await Context.Guild.GetUserAsync(Context.User.Id);

            return await Task.FromResult(
                User.RoleIds.Contains((Context as CommandModule).BotConfiguration.ModeratorRoleID)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("Hiya! It seems like you don't have access to this command. Please check that you have the **moderator** role.")
            );
        }
    }

    public sealed class RequireAdministrator : PreconditionAttribute {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services) {
            IGuildUser User = await Context.Guild.GetUserAsync(Context.User.Id);

            return await Task.FromResult(
                User.RoleIds.Contains((Context as CommandModule).BotConfiguration.AdminitratorRoleID)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("Hiya! It seems like you don't have access to this command. Please check that you have the **administrator** role.")
            );
        }
    }
}
