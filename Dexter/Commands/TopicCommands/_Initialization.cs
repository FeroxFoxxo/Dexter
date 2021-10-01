using Dexter.Abstractions;
using Dexter.Databases.FunTopics;
using Dexter.Databases.UserRestrictions;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands within the Fun module.
    /// </summary>

    public partial class TopicCommands : DiscordModule
    {

        /// <summary>
        /// Loads the database containing topics for the <c>~topic</c> command.
        /// </summary>

        public FunTopicsDB FunTopicsDB { get; set; }

        /// <summary>
        /// Holds relevant information about permissions and restrictions for specific users and services.
        /// </summary>

        public RestrictionsDB RestrictionsDB { get; set; }

    }

}
