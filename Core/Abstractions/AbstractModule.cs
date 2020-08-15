using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;

namespace Dexter.Core {
    public class AbstractModule : ModuleBase<SocketCommandContext> {
        public static EmbedBuilder BuildEmbed(Thumbnails thumbnails) => new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithThumbnailUrl(thumbnails == Thumbnails.Null ? "" : ((string[])JSONConfig.Get(typeof(BotConfiguration), "ThumbnailURLs")) [(int) thumbnails]);
    }
}
