using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("nuzzle")]
        [Summary("Nuzzles a mentioned user or yourself.")]
        [Alias("nuzzles")]
        public async Task NuzzleCommand() {
            await NuzzleCommand(Context.Guild.GetUser(Context.User.Id));
        }

        [Command("nuzzle")]
        [Summary("Nuzzles a mentioned user or yourself.")]
        [Alias("nuzzles")]
        public async Task NuzzleCommand(IGuildUser User) {
            await Context.Channel.SendMessageAsync($"*nuzzles {User.Mention} floofily*");
        }

    }
}
