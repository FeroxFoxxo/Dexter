using System;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{

    public partial class MuzzleCommands
    {

        /// <summary>
        /// Has a chance to muzzle Context.User.
        /// </summary>
        /// <remarks>The probability is set to 1 in 4 - 25%.</remarks>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("roulette")]
        [Summary("Test your luck with a 1 in 4 chance you get muzzled!")]
        [Alias("testmyluck")]
        [CommandCooldown(45)]
        [GameChannelRestricted]

        public async Task RouletteCommand()
        {
            if (Random.Next(4) == 1)
            {
                IGuildUser MuzzledUser = Context.Guild.GetUser(Context.User.Id);

                await Muzzle(MuzzledUser);

                await Context.Channel.SendMessageAsync($"Muzzled **{MuzzledUser.Username}#{MuzzledUser.Discriminator}~!**");
            }
            else
                await Context.Channel.SendMessageAsync("You missed it - lucky you! <3");
        }

    }

}
