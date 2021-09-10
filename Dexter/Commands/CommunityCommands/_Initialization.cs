using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.CommunityEvents;
using Dexter.Databases.FunTopics;
using Dexter.Databases.Games;
using Dexter.Databases.UserProfiles;
using Dexter.Databases.UserRestrictions;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands and utilities within the Community module.
    /// </summary>

    public partial class CommunityCommands : DiscordModule
    {

        /// <summary>
        /// Works as an interface between the configuration files attached to the Community module and its commands.
        /// </summary>

        public CommunityConfiguration CommunityConfiguration { get; set; }

        /// <summary>
        /// Includes important data used in parsing certain humanized terms like dates and times.
        /// </summary>

        public LanguageConfiguration LanguageConfiguration { get; set; }

        /// <summary>
        /// The moderation commands configuration containing data about the borkday role.
        /// </summary>

        public ModerationConfiguration ModerationConfiguration { get; set; }

        /// <summary>
        /// Loads the database containing events for the <c>~event</c> command.
        /// </summary>

        public CommunityEventsDB CommunityEventsDB { get; set; }

        /// <summary>
        /// Holds information about users who have been forbidden from using this service.
        /// </summary>

        public RestrictionsDB RestrictionsDB { get; set; }

        /// <summary>
        /// Holds all relevant data about games being played on Dexter.
        /// </summary>

        public GamesDB GamesDB { get; set; }

        /// <summary>
        /// Holds all relevant data about topics loaded into Dexter's database.
        /// </summary>

        public FunTopicsDB FunTopicsDB { get; set; }

        /// <summary>
        /// Holds all relevant data about user profiles.
        /// </summary>

        public ProfilesDB ProfilesDB { get; set; }

    }

}
