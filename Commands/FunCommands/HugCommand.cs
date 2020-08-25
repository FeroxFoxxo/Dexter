using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("hug")]
        [Summary("Huggles a mentioned user or yourself.")]
        [Alias("huggle", "huggles", "hugs")]

        public async Task HugCommand([Optional] IGuildUser User) {
            if (User == null)
                User = (IGuildUser)Context.User;

            await Context.Channel.SendMessageAsync($"*huggles {User.Mention} tightly and floofily*");
        }

    }
}
