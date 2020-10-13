using Dexter.Abstractions;
using Dexter.Attributes;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("halt")]
        [Summary("Shuts me down in my entirety.")]
        [Alias("shutdown")]
        [RequireAdministrator]
        public async Task HaltCommand() {
            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle("Shutting Down")
                .WithDescription($"Haiya! I'll be going to sleep now.\nCya when I wake back up!")
                .SendEmbed(Context.Channel);
            Environment.Exit(0);
        }
        
    }
}
