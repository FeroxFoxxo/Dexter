using Microsoft.EntityFrameworkCore;

namespace Dexter.Abstractions {

    /// <summary>
    /// The Entity Database is an abstract class that all databases that run through the bot use.
    /// It creates an SQLite database per instance of this abstracted class.
    /// </summary>
    public class EntityDatabase : DbContext {

        /// <summary>
        /// The OnConfiguring method runs on the initialization of the database, and sets the database to use SQLite
        /// and for the SQLite database to be set to the name of the class.
        /// </summary>
        /// <param name="Options">The Context Options is what this method aims to configure,
        /// setting it to use SQLite and set the database name to be the class'.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder Options) => Options.UseSqlite($"Data Source=Databases/{GetType().Name}.db");

    }

}
