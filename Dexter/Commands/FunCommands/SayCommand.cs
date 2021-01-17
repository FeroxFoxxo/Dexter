using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        /// <summary>
        /// Prompts the bot to send the exact message referenced in the command.
        /// </summary>
        /// <remarks>Staff-only command.</remarks>
        /// <param name="Message">The string message to send.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("say")]
        [Summary("I do have a voice! Use the ~say command to speak *through* me!")]
        [Alias("speak")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireModerator]

        public async Task SayCommand([Remainder] string Message) {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(Message);
        }

    }

}
