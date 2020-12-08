using Dexter.Attributes.Methods;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class MuzzleCommands {

        [Command("roulette")]
        [Summary("Test your luck, it is a 1 in 4 chance you get muzzled!")]
        [Alias("testmyluck")]
        [CommandCooldown(120)]

        public async Task RouletteCommand() {
            if (Random.Next(4) == 1)
                await MuzzleCommand(Context.Guild.GetUser(Context.User.Id));
            else
                await Context.Channel.SendMessageAsync("Missed it- lucky you!");
        }

    }

}
