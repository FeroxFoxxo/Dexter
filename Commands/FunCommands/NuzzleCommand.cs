using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("nuzzle")]
        [Summary("Nuzzles a mentioned user or yourself.")]
        [Alias("nuzzles")]

        public async Task NuzzleCommand([Optional] IGuildUser User) {
            if (User == null)
                User = (IGuildUser)Context.User;

            await Context.Channel.SendMessageAsync($"*nuzzles {User.Mention} floofily*");
        }

    }
}
