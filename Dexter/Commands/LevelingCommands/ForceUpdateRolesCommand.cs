using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{
    public partial class LevelingCommands
    {

        /// <summary>
        /// Forces a specific user's roles to align with their Dexter level in case any event desyncs them.
        /// </summary>
        /// <param name="target">The target user to update roles for.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("forceupdateroles")]
        [Summary("Corrects a target user's ranked roles in case they're off-sync with their Dexter level.")]
        [RequireGreetFur]
        [CommandCooldown(60)]

        public async Task ForceUpdateRolesCommand(IGuildUser target)
        {
            if (await LevelingService.UpdateRoles(target, true))
                await Context.Channel.SendMessageAsync("Roles synced successfully!");
            else
                await Context.Channel.SendMessageAsync("No roles to synchronize!");
        }

    }
}
