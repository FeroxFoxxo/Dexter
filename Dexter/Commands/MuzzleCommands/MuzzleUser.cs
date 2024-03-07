using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Commands
{

    public partial class MuzzleCommands
    {

        /// <summary>
        /// Performs the muzzling of a target user, a timed event is set up to undo this at the adequate time.
        /// </summary>
        /// <param name="user">The target user</param>
        /// <param name="duration">The duration of the muzzle to be applied</param>
        /// <param name="overrideLonger">Whether to override a timeout which is longer than the one being applied.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task TimeoutUser(IGuildUser user, TimeSpan duration, bool overrideLonger = false)
        {
            if (overrideLonger || !user.TimedOutUntil.HasValue)
            {
                await user.ModifyAsync(prop =>
                {
                    prop.TimedOutUntil = DateTimeOffset.Now + duration;
                });
            }
            else
            {
                await user.ModifyAsync(prop =>
                {
                    DateTimeOffset final = DateTimeOffset.Now + duration;
                    DateTimeOffset? current = prop.TimedOutUntil.GetValueOrDefault();
                    prop.TimedOutUntil = (current is null || final > current) ? final : current;
                });
            }
            /*
await user.AddRolesAsync(new IRole[2] {
    user.Guild.GetRole(MuzzleConfiguration.MuzzleRoleID),
    user.Guild.GetRole(MuzzleConfiguration.ReactionMutedRoleID)
});

await CreateEventTimer(
    UnmuzzleCallback,
    new() { { "User", user.Id.ToString() } },
    (int) duration.TotalSeconds,
    TimerType.Expire
);
*/
        }

        /// <summary>
        /// Attempts to unmuzzle a target user.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "User".
        /// This value should be parsable to a <c>ulong</c>.
        /// </param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task UnmuzzleCallback(Dictionary<string, string> Parameters)
        {
            ulong UserID = Convert.ToUInt64(Parameters["User"]);

            IGuildUser User = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetUser(UserID);

            if (User == null)
            {
                return;
            }

            await Unmuzzle(User);
        }

        /// <summary>
        /// Removes timeouts from a given user.
        /// </summary>
        /// <param name="GuildUser">The target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task Unmuzzle(IGuildUser GuildUser)
        {
            await GuildUser.ModifyAsync(p => p.TimedOutUntil = DateTimeOffset.Now);
        }

    }

}
