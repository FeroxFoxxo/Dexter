using Dexter.Databases.EventTimers;
using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class MuzzleCommands {

        public async Task Muzzle (IGuildUser GuildUser) {
            await GuildUser.AddRolesAsync(new IRole[2] {
                GuildUser.Guild.GetRole(MuzzleConfiguration.MuzzleRoleID),
                GuildUser.Guild.GetRole(MuzzleConfiguration.ReactionMutedRoleID)
            });

            CreateEventTimer(
                UnmuzzleCallback,
                new () {  { "User", GuildUser.Id.ToString() } },
                MuzzleConfiguration.MuzzleDuration,
                TimerType.Expire
            );
        }

        public async Task UnmuzzleCallback(Dictionary<string, string> Parameters) {
            ulong UserID = Convert.ToUInt64(Parameters["User"]);

            IGuildUser User = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetUser(UserID);

            if (User == null)
                return;

            await Unmuzzle(User);
        }

        public async Task Unmuzzle (IGuildUser GuildUser) {
            await GuildUser.RemoveRolesAsync(new IRole[2] {
                GuildUser.Guild.GetRole(MuzzleConfiguration.MuzzleRoleID),
                GuildUser.Guild.GetRole(MuzzleConfiguration.ReactionMutedRoleID)
            });
        }

    }

}
