using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Relays {

    /// <summary>
    /// The RelayDB contains a set of relays that will be sent on repeat in a channel.
    /// </summary>
    
    public class RelayDB : Database {

        /// <summary>
        /// A table of relays that will repeat every x amount of messages.
        /// </summary>
        
        public DbSet<Relay> Relays { get; set; }

    }

}
