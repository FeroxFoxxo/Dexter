using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("ping")]
        [Summary("Displays the latency between both Discord and I.")]
        [Alias("latency")]

        public async Task PingCommand() {
            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle("Gateway Ping")
                .WithDescription($"**{Context.Client.Latency}ms**")
                .SendEmbed(Context.Channel);
        }

    }
}
