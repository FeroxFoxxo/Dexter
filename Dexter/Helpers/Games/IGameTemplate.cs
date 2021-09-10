using Dexter.Configurations;
using Dexter.Databases.Games;
using Discord;
using Discord.WebSocket;

namespace Dexter.Helpers.Games
{

    /// <summary>
    /// Represents a standardized form of a game, with the minimum set of globally required methods.
    /// </summary>

    public interface IGameTemplate
    {

        /// <summary>
        /// Represents the general status and data of a game.
        /// </summary>
        /// <param name="client">SocketClient used to parse UserIDs.</param>
        /// <returns>An Embed detailing the various aspects of the game in its current instance.</returns>

        public abstract EmbedBuilder GetStatus(DiscordSocketClient client);

        /// <summary>
        /// Resets the game, setting it on a default status.
        /// </summary>
        /// <param name="funConfiguration">The general module configuration, if required.</param>
        /// <param name="gamesDB">The Games database, used to reset scores if appropriate.</param>

        public abstract void Reset(FunConfiguration funConfiguration, GamesDB gamesDB);

        /// <summary>
        /// Handles a message given by the event manager when a player that is in this game sends a message in the appropriate channel.
        /// </summary>
        /// <param name="message">The message context, which includes the content and user.</param>
        /// <param name="gamesDB">The Games Database, used to modify player data if necessary.</param>
        /// <param name="client">The Discord client, used to send messages and parse users.</param>
        /// <param name="funConfiguration">The FunConfiguration parameters.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public abstract Task HandleMessage(IMessage message, GamesDB gamesDB, DiscordSocketClient client, FunConfiguration funConfiguration);

        /// <summary>
        /// Sets a local <paramref name="field"/> to a given <paramref name="value"/>.
        /// </summary>
        /// <param name="field">The name of the field to modify.</param>
        /// <param name="value">The value to set the field to.</param>
        /// <param name="funConfiguration">Holds relevant settings for specific features within the module.</param>
        /// <param name="feedback">In case this operation wasn't possible, its reason, or useful feedback even if the operation was successful.</param>
        /// <returns><see langword="true"/> if the operation was successful, otherwise <see langword="false"/>.</returns>

        public abstract bool Set(string field, string value, FunConfiguration funConfiguration, out string feedback);

        /// <summary>
        /// Gets the general information, format, additional set parameters and playing Guide for the game.
        /// </summary>
        /// <param name="funConfiguration">The general module configuration, if required.</param>
        /// <returns>An EmbedBuilder containing all essential information to run and play the game.</returns>

        public abstract EmbedBuilder Info(FunConfiguration funConfiguration);

    }
}
