using Dexter.Attributes;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class FunCommands {

        [Command("say")]
        [Summary("I now have a voice! Use the ~say command so speak *through* me!")]
        [Alias("speak")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireModerator]

        public async Task SayCommand([Remainder] string Message) {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(Message);
        }

    }
}
