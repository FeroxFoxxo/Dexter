using System;
using System.Threading.Tasks;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Humanizer;
using System.Diagnostics;

namespace Dexter.Commands
{

    public partial class UtilityCommands
    {

        /// <summary>
        /// Displays the amount of time the bot's current instance has been running for.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("uptime")]
        [Summary("Displays the amount of time that the bot has been running for.")]
        [Alias("runtime")]

        public async Task UptimeCommand()
        {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Uptime")
                .WithDescription($"I've been runnin' for **{(DateTime.Now - Process.GetCurrentProcess().StartTime).Humanize()}**~!\n*yawns*")
                .SendEmbed(Context.Channel);
        }

    }

}
