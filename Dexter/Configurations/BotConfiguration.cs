using Dexter.Abstractions;

namespace Dexter.Configurations {

    /// <summary>
    /// The BotConfiguration specifies global traits that the whole bot encompasses and requires.
    /// </summary>
    
    public class BotConfiguration : JSONConfig {

        /// <summary>
        /// The TOKEN is the string of characters that the bot uses to log into its account. It is unique per bot.
        /// </summary>
        
        public string Token { get; set; }

        /// <summary>
        /// The PREFIX is the character that the bot uses to identify the message is a command input from a user.
        /// </summary>
        
        public string Prefix { get; set; }

        /// <summary>
        /// The MODERATOR ROLE ID is the snowflake ID of the role that the moderators have.
        /// </summary>
        
        public ulong ModeratorRoleID { get; set; }

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
        /// The DEVELOPER MENTION is the string which contains the ping of the developers role in the case of an error.
        /// </summary>
        
        public string DeveloperMention { get; set; }

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
        /// The STORAGE CHANNEL ID specifies the snowflake ID of the channel Dexter will store images of messages sent.
        /// </summary>

        public ulong StorageChannelID { get; set; }

    }

}
