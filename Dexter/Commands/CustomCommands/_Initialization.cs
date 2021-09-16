using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.CustomCommands;

namespace Dexter.Commands
{

    /// <summary>
    /// The CustomCommands module relates to the addition, removal, editing and listing of custom commands.
    /// </summary>

    public partial class CustomCommands : DiscordModule
    {

        /// <summary>
        /// The CustomCommandDB contains all the custom commands that has been added to the bot.
        /// </summary>
        public CustomCommandDB CustomCommandDB { get; set; }

        /// <summary>
        /// The relevant configuration required to manage and interact with custom commands.
        /// </summary>

        public CustomCommandsConfiguration CustomCommandsConfiguration { get; set; }

    }

}