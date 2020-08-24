using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;

namespace Dexter.Core.Abstractions {
    public abstract class AbstractModule : ModuleBase<SocketCommandContext> {
        protected BotConfiguration BotConfiguration;

        protected AbstractModule(BotConfiguration _BotConfiguration) {
            BotConfiguration = _BotConfiguration;
        }

        public EmbedBuilder BuildEmbed(EmojiEnum thumbnails) => new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithThumbnailUrl(BotConfiguration.ThumbnailURLs[(int) thumbnails]);
    }
}
