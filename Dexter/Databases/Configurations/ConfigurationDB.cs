using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Configurations {

    /// <summary>
    /// The ConfigurationDB contains a set of configurations of whether a module has been enabled or disabled.
    /// </summary>
    
    public class ConfigurationDB : Database {

        /// <summary>
        /// A table of the configurations of the modules in the ConfigurationDB database.
        /// </summary>
        
        public DbSet<Configuration> Configurations { get; set; }

    }

}
