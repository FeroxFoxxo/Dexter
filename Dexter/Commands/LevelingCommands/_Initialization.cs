using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Levels;
using Dexter.Services;

namespace Dexter.Commands
{

    /// <summary>
    /// Contains all commands and utilities related to the levels system.
    /// </summary>
    public partial class LevelingCommands : DiscordModule
    {

        /// <summary>
        /// Holds specific methods for managing user levels.
        /// </summary>

        public LevelingService LevelingService { get; set; }

        /// <summary>
        /// Holds all configuration specifically relevant to the Leveling System.
        /// </summary>

        public LevelingConfiguration LevelingConfiguration { get; set; }

        /// <summary>
        /// Holds all user data relevant to user levels.
        /// </summary>

        public LevelingDB LevelingDB { get; set; }

    }
}
