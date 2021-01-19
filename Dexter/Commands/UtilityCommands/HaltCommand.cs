using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        /// <summary>
        /// Shuts the bot down and, if in use with continuous integration, restarts the process.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("halt")]
        [Summary("Shuts the bot down and, if in use with continuous intergration, restarts the process.")]
        [Alias("shutdown")]
        [RequireAdministrator]

        public async Task HaltCommand() {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Shutting Down")
                .WithDescription($"Haiya! I'll be going to sleep now.\nCya when I wake back up!")
                .SendEmbed(Context.Channel);

            Environment.Exit(0);
        }
        
    }

}
