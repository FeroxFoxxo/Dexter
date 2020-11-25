using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("ping")]
        [Summary("Displays the latency between both Discord and the bot.")]
        [Alias("latency")]

        public async Task PingCommand() {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Gateway Ping")
                .WithDescription($"**{Context.Client.Latency}ms**")
                .SendEmbed(Context.Channel);
        }

    }
}
