using Dexter.Core;
using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("version")]
        [Summary("Displays my current version <3")]
        [Alias("v")]

        public async Task VersionCommand() {
            await Context.BuildEmbed(EmojiEnum.Love)
                .WithTitle("Bot Version")
                .WithDescription($"Hello? is anyone out there -\nThis is **{BotConfiguration.Bot_Name} v{InitializeDependencies.Version}** running **Discord.NET v{DiscordConfig.Version}**")
                .SendEmbed(Context.Channel);
        }

    }
}
