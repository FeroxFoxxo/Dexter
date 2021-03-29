using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.FunTopics {

    /// <summary>
    /// The FunTopicsDB contains a set of fun topics that can be added to the database through user suggestion.
    /// </summary>
    
    public class FunTopicsDB : Database {

        /// <summary>
        /// A table of the topics in the FunTopicsDB database.
        /// </summary>
        
        public DbSet<FunTopic> Topics { get; set; }

    }

}
