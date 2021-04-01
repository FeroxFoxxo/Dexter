using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Databases.Games {

    /// <summary>
    /// Has information about conversion from natural language to internal Enums.
    /// </summary>

    public static class GameTypeConversion {

        /// <summary>
        /// Converts a lowercase string without spaces into the appropriate game.
        /// </summary>

        public static readonly Dictionary<string, GameType> GameNames = new Dictionary<string, GameType>() {
            {"hangman", GameType.Hangman }
            //{"wordchain", GameType.Shiritori},
            //{"shiritori", GameType.Shiritori}
        };

        /// <summary>
        /// Correlates each GameType with an emoji for a very small representation.
        /// </summary>

        public static readonly Dictionary<GameType, string> GameEmoji = new Dictionary<GameType, string>() {
            {GameType.Unselected, "❓"},
            {GameType.Hangman, "💀"}
            //{GameType.Shiritori, "⛓"},
            //{GameType.Charades, "🎭"},
        };

    }
}
