using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.CustomCommands
{

    /// <summary>
    /// The CustomCommandDB contains a set of custom commands that the bot will reply with once a command has been run.
    /// </summary>

    public class CustomCommandDB : Database
    {

        /// <summary>
        /// A table of the custom commands in the CustomCommandDB database.
        /// </summary>

        public DbSet<CustomCommand> CustomCommands { get; set; }

    }

}
