using System.Collections.Generic;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Discord;
using Discord.WebSocket;

namespace Dexter.Services
{
    /// <summary>
    /// The RoleManagementService adds the given roles to the user if they join the server.
    /// </summary>

    public class RoleManagementService : Service
    {
        /// <summary>
        /// Holds all relevant information about color and patreon roles.
        /// </summary>
        public UtilityConfiguration UtilityConfiguration { get; set; }

        /// <summary>
        /// Runs after dependency injection and wires up all relevant events for this service to run properly.
        /// </summary>
        public override void Initialize()
        {
            DiscordShardedClient.GuildMemberUpdated += CheckUserColorRoles;
        }

        private async Task CheckUserColorRoles(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
        {
            if (before.Value.Roles == after.Roles) return;

            int tierAfter = Commands.UtilityCommands.GetRoleChangePerms(after, UtilityConfiguration);

            List<SocketRole> toRemove = new();
            foreach (SocketRole role in after.Roles)
            {
                int roleTier = Commands.UtilityCommands.GetColorRoleTier(role.Id, UtilityConfiguration);
                if (role.Name.StartsWith(UtilityConfiguration.ColorRolePrefix)
                    && roleTier < tierAfter)
                {
                    toRemove.Add(role);
                }
            }

            if (toRemove.Count > 0)
                await after.RemoveRolesAsync(toRemove);
        }

    }
}
