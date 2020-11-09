using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class CustomCommands {

        [Command("cclist")]
        [Summary("Displays a list of custom commands.")]
        [Alias("customcommands", "ccl")]

        public async Task DisplayCommandsAsync() {
            string CustomCommands = string.Join("\n", CustomCommandDB.CustomCommands.AsQueryable().Select(CustomCommand => BotConfiguration.Prefix + CustomCommand.CommandName).ToArray());

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Here is a list of usable commands! <3")
                .WithDescription(CustomCommands.Length > 0 ? CustomCommands : "No custom commands created!")
                .SendEmbed(Context.Channel);
        }

    }
}