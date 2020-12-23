using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Dexter.Databases.CustomCommands {

    /// <summary>
    /// The CustomCommandDB contains a set of custom commands that the bot will reply with once a command has been run.
    /// </summary>
    
    public class CustomCommandDB : Database {

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
            CustomCommand GetCommandByName = CustomCommands.Find(Name);

            if (GetCommandByName != null)
                return GetCommandByName;

            CustomCommand[] GetCommandsByAlias = CustomCommands.AsQueryable().Where(CustomCMD => CustomCMD.Alias.Contains(Name)).ToArray();

            foreach (CustomCommand Command in GetCommandsByAlias)
                if (JsonConvert.DeserializeObject<List<string>>(Command.Alias).Contains(Name))
                    return Command;

            return null;
        }

    }

}
