using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.FunTopics;
using Dexter.Databases.Games;
using Dexter.Databases.UserRestrictions;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands within the Fun module.
    /// </summary>

    public partial class FunCommands : DiscordModule
    {

        /// <summary>
        /// Works as an interface between the configuration files attached to the Fun module and the commands.
        /// </summary>

        public FunConfiguration FunConfiguration { get; set; }

        /// <summary>
        /// Holds relevant information about permissions and restrictions for specific users and services.
        /// </summary>

        public RestrictionsDB RestrictionsDB { get; set; }

        /// <summary>
        /// Holds all relevant data about games being played on Dexter.
        /// </summary>

        public GamesDB GamesDB { get; set; }

    }

}
