using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Abstractions {

    /// <summary>
    /// The DiscordModule class is an abstract class all command modules extend upon.
    /// Command modules contain methods that run on the specified command being entered.
    /// </summary>
    public abstract class DiscordModule : ModuleBase<SocketCommandContext> {

        /// <summary>
        /// The Build Embed method is a generic method that simply calls upon the EMBED BUILDER extension method.
        /// </summary>
        /// <param name="Thumbnail">The thumbnail that you would like to be applied to the embed.</param>
        /// <returns>A new embed builder with the specified attributes applied to the embed.</returns>
        public static EmbedBuilder BuildEmbed(EmojiEnum Thumbnail) {
            return new EmbedBuilder().BuildEmbed(Thumbnail);
        }

    }

}
