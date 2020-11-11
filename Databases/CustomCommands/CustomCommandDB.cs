using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Dexter.Databases.CustomCommands {

    /// <summary>
    /// The CustomCommandDB contains a set of custom commands that the bot will reply with once a command has been run.
    /// </summary>
    public class CustomCommandDB : EntityDatabase {

        /// <summary>
        /// A table of the custom commands in the CustomCommandDB database.
        /// </summary>
        public DbSet<CustomCommand> CustomCommands { get; set; }

        /// <summary>
        /// The Get Command By Name Or Alias method gets the CustomCommand object from the CustomCommands table
        /// that matches an alias or the command name itself through a query of the database.
        /// </summary>
        /// <param name="Name">The name of the command or alias you wish to query for.</param>
        /// <returns>A CustomCommand object of the command you queried.</returns>
        public CustomCommand GetCommandByNameOrAlias(string Name) {
            return CustomCommands.AsQueryable()
                        .Where(CustomCMD => CustomCMD.CommandName == Name || CustomCMD.Alias.Contains(Name))
                        .FirstOrDefault();
        }

    }

}
