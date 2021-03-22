using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Borkdays;
using Dexter.Databases.Mail;
using Dexter.Databases.Reminders;
using Dexter.Services;

namespace Dexter.Commands {

    /// <summary>
    /// The class containing all commands within the Utility module.
    /// </summary>

    public partial class UtilityCommands : DiscordModule {

        /// <summary>
        /// Allows logging necessary data pertaining to issues or important information during interaction with the command environment.
        /// </summary>

        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// Holds all relevant settings and configuration for the Utility Commands Module.
        /// </summary>
        
        public UtilityConfiguration UtilityConfiguration { get; set; }

        /// <summary>
        /// The BorkdayDB stores information regarding a user's birthday.
        /// </summary>

        public BorkdayDB BorkdayDB { get; set; }

        /// <summary>
        /// The ModmailDB stores information about the mailing service and mailed messages.
        /// </summary>

        public ModMailDB ModMailDB { get; set; }

        /// <summary>
        /// Stores information relevant to the Reminder system.
        /// </summary>

        public ReminderDB ReminderDB { get; set; }

        /// <summary>
        /// Coordinates the initialization of all necessary infrastructure upon startup.
        /// </summary>

        public StartupService StartupService { get; set; }

        /// <summary>
        /// Contains information relative to organic language management and time zones.
        /// </summary>

        public LanguageConfiguration LanguageConfiguration { get; set; }
    }

}
