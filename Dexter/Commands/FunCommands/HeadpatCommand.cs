using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("headpat", RunMode = RunMode.Async)]
        [Summary("Oop, you've been a good boy? *gives rapid headpats through a gif send in the chat from your user*")]
        [Alias("headpats", "petpat", "petpats")]
        [CommandCooldown(180)]

        public async Task HeadpatCommand([Optional] IUser User) {

        }

    }

}
