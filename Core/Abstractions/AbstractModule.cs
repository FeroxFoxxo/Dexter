using Dexter.Core.Enums;
using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;

namespace Dexter.Core {
    public abstract class AbstractModule : ModuleBase<SocketCommandContext> {
        protected JSONConfig JSONConfig;

        protected AbstractModule(JSONConfig _JSONConfig) {
            JSONConfig = _JSONConfig;
        }

        public EmbedBuilder BuildEmbed(EmojiEnum thumbnails) => new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithThumbnailUrl(((string[])JSONConfig.Get(typeof(BotConfiguration), "ThumbnailURLs")) [(int) thumbnails]);
    }
}
