using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.GreetFur;
using Dexter.Databases.UserProfiles;
using Dexter.Events;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands within the GreetFur module.
    /// </summary>

    public partial class GreetFurCommands : DiscordModule
	{

		/// <summary>
		/// Works as an interface between the configuration files attached to the GreetFur module and the commands.
		/// </summary>

		public GreetFurConfiguration GreetFurConfiguration { get; set; }

		/// <summary>
		/// A Configuration file containing linguistic information used to parse time zones.
		/// </summary>

		public LanguageConfiguration LanguageConfiguration { get; set; }

		/// <summary>
		/// A wrapper for useful GreetFur data and a wrapper for spreadsheet manipulation and access.
		/// </summary>

		public GreetFur GreetFurService { get; set; }

		/// <summary>
		/// A database with information about GreetFur activity records.
		/// </summary>

		public GreetFurDB GreetFurDB { get; set; }

		/// <summary>
		/// A database with information about GreetFur profile preferences.
		/// </summary>

		public ProfilesDB ProfilesDB { get; set; }
	}

}
