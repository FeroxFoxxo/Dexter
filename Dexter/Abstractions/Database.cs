using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace Dexter.Abstractions
{

    /// <summary>
    /// Database is an abstract class that all databases that run through the bot use.
    /// It creates an SQLite database per instance of this abstracted class.
    /// </summary>

    public class Database : DbContext
    {

        /// <summary>
        /// The OnConfiguring method runs on the initialization of the database, and sets the database to use SQLite
        /// and for the SQLite database to be set to the name of the class.
        /// </summary>
        /// <param name="options">The Context Options is what this method aims to configure,
        /// setting it to use SQLite and set the database name to be the class'.</param>

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!string.IsNullOrEmpty(Startup.DBUser) && !string.IsNullOrEmpty(Startup.DBPassword))
                options.UseMySQL($"server=localhost;database={GetType().Name};user={Startup.DBUser};password={Startup.DBPassword}");
            else
                options.UseSqlite($"Data Source=Databases/{GetType().Name}.db");
        }

    }

}
