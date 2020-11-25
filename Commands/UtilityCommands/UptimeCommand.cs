using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Humanizer;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("uptime")]
        [Summary("Displays the amount of time that the bot has been running for.")]
        [Alias("runtime")]

        public async Task UptimeCommand() {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Uptime")
                .WithDescription($"I've been runnin' for **{(DateTime.Now - Process.GetCurrentProcess().StartTime).Humanize()}**~!\n*yawns*")
                .SendEmbed(Context.Channel);
        }

    }
}
