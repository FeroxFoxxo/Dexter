using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Databases.CustomCommands;

namespace Dexter.Commands {

    /// <summary>
    /// The CustomCommands module relates to the addition, removal, editing and listing of custom commands.
    /// </summary>
    public partial class CustomCommands : DiscordModule {

        private readonly CustomCommandDB CustomCommandDB;
        private readonly BotConfiguration BotConfiguration;

        /// <summary>
        /// The constructor for the CustomCommands module. This takes in the injected dependencies and sets them as per what the class requires.
        /// </summary>
        /// <param name="CustomCommandDB">The CustomCommandDB contains all the custom commands that has been added to the bot.</param>
        /// <param name="BotConfiguration">The BotConfiguration contains reference to the prefix of the bot in the server.</param>
        public CustomCommands(CustomCommandDB CustomCommandDB, BotConfiguration BotConfiguration) {
            this.CustomCommandDB = CustomCommandDB;
            this.BotConfiguration = BotConfiguration;
        }

    }

}