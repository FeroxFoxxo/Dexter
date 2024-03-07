using Dexter.Attributes;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands
{

    public partial class MuzzleCommands
    {

        /// <summary>
        /// Has a chance to muzzle Context.User.
        /// </summary>
        /// <remarks>The probability is set to 1 in 4 - 25%.</remarks>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("roulette", ignoreExtraArgs: true)]
        [Summary("Test your luck with a 1 in 4 chance you get muzzled!")]
        [Alias("testmyluck")]
        [CommandCooldown(45)]
        [GameChannelRestricted]

        public async Task RouletteCommand()
        {
            if (Random.Next(4) == 1)
            {
                IGuildUser targetUser = Context.Guild.GetUser(Context.User.Id);

                await TimeoutUser(targetUser, TimeSpan.FromSeconds(MuzzleConfiguration.MuzzleDuration));

                await Context.Channel.SendMessageAsync($"Muzzled **{targetUser.Username}~!**");
            }
            else
            {
                await Context.Channel.SendMessageAsync("You missed it - lucky you! <3");
            }
        }

    }

}
