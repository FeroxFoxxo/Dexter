using Dexter.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Abstractions {
    public class CommandModule : SocketCommandContext {
        public BotConfiguration BotConfiguration;

        public CommandModule(DiscordSocketClient Client, SocketUserMessage Message, BotConfiguration _BotConfiguration) : base(Client, Message) {
            BotConfiguration = _BotConfiguration;
        }

        public EmbedBuilder BuildEmbed(EmojiEnum Thumbnails) => new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithThumbnailUrl(BotConfiguration.ThumbnailURLs[(int)Thumbnails]);
    }
}
