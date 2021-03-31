using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Helpers.Games {

    /// <summary>
    /// Represents a standardized form of a game, with the minimum set of globally required methods.
    /// </summary>

    public interface IGameTemplate {

        /// <summary>
        /// Represents the general status and data of a game.
        /// </summary>
        /// <param name="Client">SocketClient used to parse UserIDs.</param>
        /// <returns>An Embed detailing the various aspects of the game in its current instance.</returns>

        public abstract EmbedBuilder GetStatus(DiscordSocketClient Client);

        /// <summary>
        /// Resets the game, setting it on a default status.
        /// </summary>
        /// <param name="FunConfiguration">The general module configuration, if required.</param>

        public abstract void Reset(FunConfiguration FunConfiguration);

        /// <summary>
        /// Handles a message given by the event manager when a player that is in this game sends a message in the appropriate channel.
        /// </summary>
        /// <param name="Message">The message context, which includes the content and user.</param>
        /// <param name="GamesDB">The Games Database, used to modify player data if necessary.</param>
        /// <param name="Client">The Discord client, used to send messages and parse users.</param>
        /// <param name="FunConfiguration">The FunConfiguration parameters.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public abstract Task HandleMessage(IMessage Message, GamesDB GamesDB, DiscordSocketClient Client, FunConfiguration FunConfiguration);

        /// <summary>
        /// Sets a local <paramref name="Field"/> to a given <paramref name="Value"/>.
        /// </summary>
        /// <param name="Field">The name of the field to modify.</param>
        /// <param name="Value">The value to set the field to.</param>
        /// <param name="FunConfiguration">Holds relevant settings for specific features within the module.</param>
        /// <param name="Feedback">In case this operation wasn't possible, its reason, or useful feedback even if the operation was successful.</param>
        /// <returns><see langword="true"/> if the operation was successful, otherwise <see langword="false"/>.</returns>

        public abstract bool Set(string Field, string Value, FunConfiguration FunConfiguration, out string Feedback);

        /// <summary>
        /// Gets the general information, format, additional set parameters and playing Guide for the game.
        /// </summary>
        /// <param name="FunConfiguration">The general module configuration, if required.</param>
        /// <returns>An EmbedBuilder containing all essential information to run and play the game.</returns>

        public abstract EmbedBuilder Info(FunConfiguration FunConfiguration);

    }
}
