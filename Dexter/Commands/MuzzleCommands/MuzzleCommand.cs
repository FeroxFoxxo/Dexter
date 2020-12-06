using Dexter.Attributes.Methods;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class MuzzleCommands {

        [Command("muzzle")]
        [Summary("Issue the command, and s i l e n c e ,  T H O T-!")]
        [Alias("muzzleme")]
        [CommandCooldown(60)]

        public async Task MuzzleCommand() {

        }

    }

}
