using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("sfw")]
        [Summary("A safe for work command for when a fluff's been a touch - *ehhh*.")]
        [Alias("safeforwork", "safe-for-work", "safe for work")]
        public async Task SFWCommand() {
            await Context.Channel.SendMessageAsync("Firstly, SFW.\nSecondly: ***what the fuck?!?***");
        }

        [Command("sfw")]
        [Summary("A safe for work command for when a fluff's been a touch - *ehhh*.")]
        [Alias("safeforwork", "safe-for-work", "safe for work")]
        public async Task SFWCommand(IGuildUser User) {
            await Context.Channel.SendMessageAsync($"Firstly, SFW {User.Mention}.\nSecondly: ***what the fuck?!?***");
        }

    }
}
