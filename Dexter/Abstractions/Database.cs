using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

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
			string conString = $"server=localhost;database={GetType().Name};user={Startup.DBUser};password={Startup.DBPassword}";

			if (!string.IsNullOrEmpty(Startup.DBUser) && !string.IsNullOrEmpty(Startup.DBPassword))
				options.UseMySql(conString, ServerVersion.AutoDetect(conString));
			else
				options.UseSqlite($"Data Source=Databases/{GetType().Name}.db");
		}

		private const int RETRIES = 5;

		/// <summary>
		/// Using a retry system to ensure that the 
		/// </summary>
		/// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully or throws an exception.</returns>
		/// <exception cref="DbUpdateException"></exception>

		public async Task EnsureSaved()
        {
			for (int i = 0; i < RETRIES; i++)
			{
				try
				{
					SaveChanges();
					return;
				}
				catch
				{
					Console.WriteLine($"Failed to save {GetType().Name}, attempt {i + 1}/{RETRIES}");
					await Task.Delay(50);
				}
			}

			throw new DbUpdateException($"Unable to save {GetType().Name} after {RETRIES} attempts.");
		}

	}

}
