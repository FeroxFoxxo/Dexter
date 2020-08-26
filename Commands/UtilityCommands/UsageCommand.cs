using Dexter.Core.Abstractions;
using Discord.Commands;
using Humanizer;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("usage")]
        [Summary("Displays the amount of time I have been running for!")]
        [Alias("memspec", "cpuspec")]
        public async Task UsageCommand() {
            Process Process = Process.GetCurrentProcess();

            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle("Usage Statistics")
                .AddField("Base priority", Process.BasePriority)
                .AddField("Priority class", Process.PriorityClass)
                .AddField("Physical memory usage", $"{Process.WorkingSet64 / (1024 * 1024)} MBs")
                .AddField("Paged memory size", $"{Process.PagedMemorySize64 / (1024 * 1024)} MBs")
                .AddField("Paged system memory size", $"{Process.PagedSystemMemorySize64 / 1024} KBs")
                .AddField("User processor time", Process.UserProcessorTime.Humanize())
                .AddField("Privileged processor time", Process.PrivilegedProcessorTime.Humanize())
                .AddField("Total processor time", Process.TotalProcessorTime.Humanize())
                .SendEmbed(Context.Channel);
        }

    }
}
