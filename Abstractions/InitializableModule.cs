using Dexter.Enums;
using Dexter.Extensions;
using Discord;

namespace Dexter.Abstractions {
    public abstract class InitializableModule {

        public abstract void AddDelegates();

        public static EmbedBuilder BuildEmbed(EmojiEnum Thumbnail) {
            return new EmbedBuilder().BuildEmbed(Thumbnail);
        }

    }
}
