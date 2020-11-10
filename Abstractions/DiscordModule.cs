using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Abstractions {
    public abstract class DiscordModule : ModuleBase<SocketCommandContext> {

        public static EmbedBuilder BuildEmbed(EmojiEnum Thumbnail) {
            return new EmbedBuilder().BuildEmbed(Thumbnail);
        }

    }
}
