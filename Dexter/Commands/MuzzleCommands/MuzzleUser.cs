using Dexter.Databases.EventTimers;
using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class MuzzleCommands {

        /// <summary>
        /// Performs the muzzling of a target user, a timed event is set up to undo this at the adequate time.
        /// </summary>
        /// <param name="GuildUser">The target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task Muzzle (IGuildUser GuildUser) {
            await GuildUser.AddRolesAsync(new IRole[2] {
                GuildUser.Guild.GetRole(MuzzleConfiguration.MuzzleRoleID),
                GuildUser.Guild.GetRole(MuzzleConfiguration.ReactionMutedRoleID)
            });

            await CreateEventTimer(
                UnmuzzleCallback,
                new () {  { "User", GuildUser.Id.ToString() } },
                MuzzleConfiguration.MuzzleDuration,
                TimerType.Expire
            );
        }

        /// <summary>
        /// Attempts to unmuzzle a target user.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "User".
        /// This value should be parsable to a <c>ulong</c>.
        /// </param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task UnmuzzleCallback(Dictionary<string, string> Parameters) {
            ulong UserID = Convert.ToUInt64(Parameters["User"]);

            IGuildUser User = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetUser(UserID);

            if (User == null)
                return;

            await Unmuzzle(User);
        }

        /// <summary>
        /// Removes the "Muzzled" and "Reaction Muted" role from a given user.
        /// </summary>
        /// <param name="GuildUser">The target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task Unmuzzle (IGuildUser GuildUser) {
            await GuildUser.RemoveRolesAsync(new IRole[2] {
                GuildUser.Guild.GetRole(MuzzleConfiguration.MuzzleRoleID),
                GuildUser.Guild.GetRole(MuzzleConfiguration.ReactionMutedRoleID)
            });
        }

    }

}
