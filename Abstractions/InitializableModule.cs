using Dexter.Enums;
using Dexter.Extensions;
using Discord;

namespace Dexter.Abstractions {

    /// <summary>
    /// The Initializable Module is an abstract class that all services extend upon.
    /// Services run when a specified event occured, as is hooked into the client through the Add Delegates method.
    /// </summary>
    public abstract class InitializableModule {

        /// <summary>
        /// The Add Delegates abstract method is what is called when all dependencies are initialized.
        /// It can be used to hook into delegates to run when an event occurs.
        /// </summary>
        public abstract void AddDelegates();

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
