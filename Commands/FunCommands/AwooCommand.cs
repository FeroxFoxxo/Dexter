using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("awoo")]
        [Summary("AWOO!!!")]
        [Alias("awo", "awooo")]
        public async Task AwooCommand() {
            await Context.Channel.SendMessageAsync("**AWOO!**");
        }

    }
}
