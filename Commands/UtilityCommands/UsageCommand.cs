using Dexter.Core.Abstractions;
using Dexter.Core.DiscordApp;
using Discord.Commands;
using Humanizer;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("usage")]
        [Summary("Displays the amount of time I have been running for!")]
        [Alias("memspec", "cpuspec")]
        [RequireAdministrator]
        public async Task UsageCommand() {
            Process Process = Process.GetCurrentProcess();
            string CPUs = string.Empty;

            foreach (ManagementObject CPU in new ManagementObjectSearcher("select * from Win32_Processor").Get())
                CPUs += $"{CPU["Name"]} ";

            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle("Usage Statistics")
                .AddField("Priority", $"{Process.PriorityClass} ({Process.BasePriority})")
                .AddField("Memory size", $"{Process.PagedMemorySize64 / (1024 * 1024)} MBs ({Process.WorkingSet64 / (1024 * 1024)} Physical MBs) / {MemCounter.NextValue()} MBs")
                .AddField("User processor time", $"Used time: {Process.UserProcessorTime.Humanize()} ({Process.PrivilegedProcessorTime.Humanize()} privilaged)")
                .AddField("CPU", $"{CPUs} ({CPUCounter.NextValue()}%)")
                .SendEmbed(Context.Channel);
        }

    }
}
