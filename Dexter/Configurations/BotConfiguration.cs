using Dexter.Abstractions;

namespace Dexter.Configurations {

    /// <summary>
    /// The BotConfiguration specifies global traits that the whole bot encompasses and requires.
    /// </summary>
    
    public class BotConfiguration : JSONConfig {

        /// <summary>
        /// The PREFIX is the character that the bot uses to identify the message is a command input from a user.
        /// </summary>
        
        public string Prefix { get; set; }

        /// <summary>
        /// The MODERATOR ROLE ID is the snowflake ID of the role that the moderators have.
        /// </summary>
        
        public ulong ModeratorRoleID { get; set; }

        /// <summary>
        /// The ADMINISTRATOR ROLE ID is the snowflake ID of the role that the administrators have.
        /// </summary>

        public ulong AdministratorRoleID { get; set; }

        /// <summary>
        /// The DEVELOPER ROLE ID is the snowflake ID of the role that the developers have.
        /// </summary>

        public ulong DeveloperRoleID { get; set; }

        /// <summary>
        /// The GREETFUR ROLE ID is the snowflake ID of the role that the greetfurs have.
        /// </summary>

        public ulong GreetFurRoleID { get; set; }

        /// <summary>
        /// The GUILD ID is the snowflake ID of the main guild the bot is in.
        /// </summary>

        public ulong GuildID { get; set; }

        /// <summary>
        /// The MODERATION LOG CHANNEL ID is the snowflake ID of where the bot
        /// will post updates of his status to, along with confirmation messages.
        /// </summary>
        
        public ulong ModerationLogChannelID { get; set; }

        /// <summary>
        /// The THUMBNAIL URLS contains a list of URLs that the bot uses to attach to the default embeds.
        /// </summary>
        
        public string[] ThumbnailURLs { get; set; }

        /// <summary>
        /// The BOT CHANNELS contains a list of channel IDs in which commands labeled as only being able
        /// to be used in bot command channels are able to be used.
        /// </summary>
        
        public ulong[] BotChannels { get; set; }

        /// <summary>
        /// The DISALLOWED CHANNELS contains a list of channel IDs in which commands can not run in.
        /// </summary>

        public ulong[] DisallowedChannels { get; set; }

        /// <summary>
        /// The ENABLE STARTUP ALERT is a boolean which will enable the startup notification
        /// sent to the moderation logging channel when the bot starts up.
        /// </summary>

        public bool EnableStartupAlert { get; set; }

        /// <summary>
        /// The HELP field contains information for the help command.
        /// </summary>
        
        public string Help { get; set; }

        /// <summary>
        /// The RANDOM CHARACTERS field specifies a string of random characters that may be able to make up a token.
        /// </summary>

        public string RandomCharacters { get; set; }

        /// <summary>
        /// The TRACKER LENGTH specifies the length of the token assosiated with the tracked item.
        /// </summary>

        public int TrackerLength { get; set; }
        
        /// <summary>
        /// Stores the URL to the commits page in the relevant GIT repository.
        /// </summary>

        public string CommitAPICall { get; set; }

        /// <summary>
        /// Sets the status of the bot to the one provided in the string.
        /// </summary>

        public string BotStatus { get; set; }

        /// <summary>
        /// Configures whether periodic backups of all databases should be made.
        /// </summary>

        public bool EnableDatabaseBackups { get; set; }

        /// <summary>
        /// Sets if the developers should be pinged when the bot runs into an error.
        /// </summary>

        public bool PingDevelopers { get; set; }

        /// <summary>
        /// The standardized timezone to parse timezone-sensitive DateTimeOffsets to.
        /// </summary>

        public short StandardTimeZone { get; set; }

    }

}
