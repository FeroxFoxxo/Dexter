using Dexter.Configurations;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;

namespace Dexter.Abstractions {
    public abstract class InitializableModule {

        private readonly BotConfiguration BotConfiguration;

        public abstract void AddDelegates();

        public InitializableModule(BotConfiguration BotConfiguration) {
            this.BotConfiguration = BotConfiguration;
        }

        public EmbedBuilder BuildEmbed(EmojiEnum Thumbnail) {
            return new EmbedBuilder().BuildEmbed(Thumbnail, BotConfiguration);
        }

    }
}
