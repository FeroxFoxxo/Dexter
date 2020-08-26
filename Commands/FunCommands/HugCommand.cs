using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("hug")]
        [Summary("Huggles a mentioned user or yourself.")]
        [Alias("huggle", "huggles", "hugs")]
        public async Task HugCommand() {
            await HugCommand(Context.Guild.GetUser(Context.User.Id));
        }

        [Command("hug")]
        [Summary("Huggles a mentioned user or yourself.")]
        [Alias("huggle", "huggles", "hugs")]
        public async Task HugCommand(IGuildUser User) {
            await Context.Channel.SendMessageAsync($"*huggles {User.Mention} tightly and floofily*");
        }

    }
}
