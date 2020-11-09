using Dexter.Configurations;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Abstractions {
    public abstract class DiscordModule : ModuleBase<SocketCommandContext> {

        private readonly BotConfiguration BotConfiguration;

        public DiscordModule(BotConfiguration BotConfiguration) {
            this.BotConfiguration = BotConfiguration;
        }

        public EmbedBuilder BuildEmbed(EmojiEnum Thumbnail) {
            return new EmbedBuilder().BuildEmbed(Thumbnail, BotConfiguration);
        }

    }
}
