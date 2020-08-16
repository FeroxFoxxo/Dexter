using Dexter.Core.Enums;
using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;

namespace Dexter.Core {
    public class AbstractModule : ModuleBase<SocketCommandContext> {
        public static EmbedBuilder BuildEmbed(EmojiEnum thumbnails) => new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithThumbnailUrl(((string[])JSONConfig.Get(typeof(BotConfiguration), "ThumbnailURLs")) [(int) thumbnails]);
    }
}
