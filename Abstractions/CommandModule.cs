using Dexter.Configurations;
using Dexter.Enums;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Abstractions {
    public class CommandModule : SocketCommandContext {
        public BotConfiguration BotConfiguration;

        public CommandModule(DiscordSocketClient Client, SocketUserMessage Message, BotConfiguration _BotConfiguration) : base(Client, Message) {
            BotConfiguration = _BotConfiguration;
        }

        public EmbedBuilder BuildEmbed(EmojiEnum Thumbnails) {
            Color Color = Thumbnails switch {
                EmojiEnum.Annoyed => Color.Red,
                EmojiEnum.Love => Color.Green,
                EmojiEnum.Sign => Color.Blue,
                EmojiEnum.Wut => Color.Teal,
                EmojiEnum.Unknown => Color.Orange,
                _ => Color.Magenta
            };

            return new EmbedBuilder().WithThumbnailUrl(BotConfiguration.ThumbnailURLs[(int)Thumbnails]).WithColor(Color);
        }
    }
}
