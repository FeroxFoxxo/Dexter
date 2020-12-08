using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("headpat", RunMode = RunMode.Async)]
        [Summary("Ooh, you've been a good boy? *gives rapid headpats in an emoji*")]
        [Alias("headpats", "petpat", "petpats")]
        [CommandCooldown(180)]

        public async Task HeadpatCommand([Optional] IUser User) {

        }

    }

}
