using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Games;
using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Games
{

    /// <summary>
    /// Represents a particular instance of a game or session in the Dexter Games subsystem.
    /// </summary>

    public class GameInstance
    {

        /// <summary>
        /// Comma representation used to escape comma characters in managed data sequences.
        /// </summary>

        public const string CommaRepresentation = "$#44;";

        /// <summary>
        /// Unique identifier of the game instance.
        /// </summary>

        [Key]
        public int GameID { get; set; }

        /// <summary>
        /// The last time this game instance received any interactions, game instances should be closed after a while without interaction.
        /// Measured in seconds since UNIX Time.
        /// </summary>

        public long LastInteracted { get; set; }

        /// <summary>
        /// The unique ID of the last user who interacted with the game system.
        /// </summary>

        public ulong LastUserInteracted { get; set; }

        /// <summary>
        /// The game type that's being played in this instance.
        /// </summary>

        public GameType Type { get; set; }

        /// <summary>
        /// This session's title, generally shown on Embed Titles.
        /// </summary>

        public string Title { get; set; }

        /// <summary>
        /// This session's description.
        /// </summary>

        public string Description { get; set; }

        /// <summary>
        /// A password for this game instance, required for joining if different from <see cref="string.Empty"/>.
        /// </summary>

        public string Password { get; set; }

        /// <summary>
        /// This game's Game Master (or Host) who has control over it.
        /// </summary>

        public ulong Master { get; set; }

        /// <summary>
        /// A list of Comma-separated UserIDs for users that are banned from joining this instance.
        /// </summary>

        public string Banned { get; set; }

        /// <summary>
        /// Gets the banned users in a more user-friendly way.
        /// </summary>

        public string BannedMentions
        {
            get
            {
                return Banned.Length > 0 ? $"<@{Banned.Replace(", ", "> <@")}>" : "";
            }
        }

        /// <summary>
        /// Any additional game-specific data managed at a per-<see cref="GameTemplate"/> level.
        /// </summary>

        public string Data { get; set; }


        /// <summary>
        /// Converts the generic GameInstance into the <see cref="GameTemplate"/> indicated by <see cref="Type"/>
        /// </summary>
        /// <returns>The <see cref="GameTemplate"/> corresponding to this GameInstance, or <see langword="null"/> if the gameType is unknown.</returns>

        public GameTemplate ToGameProper(BotConfiguration botConfiguration)
        {
            return (Type) switch
            {
                GameType.Hangman => new GameHangman(this, botConfiguration),
                GameType.TicTacToe => new GameTicTacToe(this, botConfiguration),
                GameType.Connect4 => new GameConnect4(this, botConfiguration),
                GameType.Minesweeper => new GameMinesweeper(this, botConfiguration),
                GameType.Chess => new GameChess(this, botConfiguration),
                _ => null
            };
        }

    }

    /// <summary>
    /// Represents an integer that has a set range from which it can't deviate.
    /// </summary>

    public struct BoundedInt
    {
        /// <summary>
        /// The minimum permitted value for the integer
        /// </summary>
        public int min;
        /// <summary>
        /// The maximum permitted value for the integer
        /// </summary>
        public int max;
        private int value;

        /// <summary>
        /// The value this integer represents
        /// </summary>
        public int Value
        {
            get
            {
                return value;
            }
            set
            {
                if (value > max) this.value = max;
                else if (value < min) this.value = min;
                else this.value = value;
            }
        }

        /// <summary>
        /// Implicit conversion to an integer.
        /// </summary>
        /// <param name="b">The bounded integer to convert.</param>
        public static implicit operator int(BoundedInt b) => b.Value;
    }
}
