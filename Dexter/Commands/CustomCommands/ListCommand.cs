using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class CustomCommands {

        /// <summary>
        /// The ListCommands method runs on CCLIST and will list all the custom commands in the database.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("cclist")]
        [Summary("Displays all avaliable custom commands in the database.")]
        [Alias("customcommands", "ccl")]

        public async Task ListCommands () {
            string CustomCommands = string.Join("\n", CustomCommandDB.CustomCommands.AsQueryable().Select(CustomCommand => BotConfiguration.Prefix + CustomCommand.CommandName));

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Here is a list of usable commands! <3")
                .WithDescription(CustomCommands.Length > 0 ? CustomCommands : "No custom commands created!")
                .SendEmbed(Context.Channel);
        }

    }

}