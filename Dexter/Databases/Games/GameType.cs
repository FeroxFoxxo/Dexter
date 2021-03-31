using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Databases.Games {

    /// <summary>
    /// Indicates a game type that a game instance can take on.
    /// </summary>

    public enum GameType {

        /// <summary>
        /// The default GameType, it should largely be unused.
        /// </summary>

        Unselected,

        /// <summary>
        /// An instance game of Hangman, a game about guessing an obscured word by guessing individual letters.
        /// </summary>

        Hangman,

        /// <summary>
        /// An instance game of Shiritori, a game about coming up with words that start by whatever the previous person's word ends.
        /// </summary>

        Shiritori,

        /// <summary>
        /// An instance game of Charades, a game about guessing what a person is trying to nonverbally communicate.
        /// </summary>

        Charades
    }
}
