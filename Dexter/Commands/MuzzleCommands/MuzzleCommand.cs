using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class MuzzleCommands {

        [Command("muzzle")]
        [Summary("Issue the command, and s i l e n c e ,  T H O T-!")]
        [Alias("muzzleme")]
        [CommandCooldown(60)]

        public async Task MuzzleCommand() {
            await MuzzleCommand(Context.Guild.GetUser(Context.User.Id));
        }

        [Command("muzzle")]
        [Summary("Issue the command, and s i l e n c e ,  T H O T-!")]
        [Alias("muzzleme")]
        [RequireModerator]

        public async Task MuzzleCommand(IGuildUser User) {
            await Muzzle(User);

            await Context.Channel.SendMessageAsync($"Muzzled **{User.Username}#{User.Discriminator}~!**");
        }

    }

}
