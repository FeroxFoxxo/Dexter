using Dexter.Abstractions;
using Dexter.Configurations;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Services
{
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
            DiscordSocketClient.GuildMemberUpdated += CheckUserColorRoles;
        }

        private async Task CheckUserColorRoles(SocketGuildUser before, SocketGuildUser after)
        {
            if (before.Roles == after.Roles) return;

            bool isAfterPatreon = false;

            foreach (ulong patreonRoleID in UtilityConfiguration.ColorChangeRoles)
            {
                if (after.Roles.Any(r => r.Id == patreonRoleID))
                {
                    isAfterPatreon = true;
                }
            }

            if (isAfterPatreon) return;

            List<SocketRole> toRemove = new();
            foreach (SocketRole role in after.Roles)
            {
                if (role.Name.StartsWith(UtilityConfiguration.ColorRolePrefix))
                {
                    toRemove.Add(role);
                }
            }

            if (toRemove.Count > 0)
                await after.RemoveRolesAsync(toRemove);
        }

    }
}
