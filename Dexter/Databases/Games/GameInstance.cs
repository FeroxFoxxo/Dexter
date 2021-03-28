using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Dexter.Databases.Games {
    public class GameInstance {

        public const string CommaRepresentation = "$#44;";

        [Key]
        public int GameID { get; set; }

        /// <summary>
        /// The last time this game instance received any interactions, game instances should be closed after a while without interaction.
        /// Measured in seconds since UNIX Time.
        /// </summary>

        public long LastInteracted { get; set; }

        public ulong LastUserInteracted { get; set; }

        public GameType Type { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Password { get; set; }

        public ulong Master { get; set; }

        public string Banned { get; set; }

        public string Data { get; set; }

    }
}
