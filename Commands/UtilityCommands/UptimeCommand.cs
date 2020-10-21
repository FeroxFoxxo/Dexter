using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Discord.Commands;
using Humanizer;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("uptime")]
        [Summary("Displays the amount of time I have been running for!")]
        [Alias("runtime")]

        public async Task UptimeCommand() {
            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle("Uptime")
                .WithDescription($"I've been runnin' for **{(DateTime.Now - Process.GetCurrentProcess().StartTime).Humanize()}**~!\n*yawns*")
                .SendEmbed(Context.Channel);
        }

    }
}
