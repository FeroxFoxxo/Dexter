using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("sleep")]
        [Summary("Sweet dreams! Use this command to be wished a good night <3")]
        [Alias("goodnight", "ninite", "gnight")]
        public async Task SleepCommand() {
            await SleepCommand(Context.Guild.GetUser(Context.User.Id));
        }

        [Command("sleep")]
        [Summary("Sweet dreams! Use this command to be wished a good night <3")]
        [Alias("goodnight", "ninite", "gnight")]
        public async Task SleepCommand(IGuildUser User) {
            await Context.Channel.SendMessageAsync($"Goodnight {User.Mention}, sleep well! :blue_heart:");
        }

    }
}
