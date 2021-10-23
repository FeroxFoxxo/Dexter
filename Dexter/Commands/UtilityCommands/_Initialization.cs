using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Reminders;
using Dexter.Databases.UserProfiles;
using Dexter.Events;
using Discord.Commands;
using System;

namespace Dexter.Commands
{

	/// <summary>
	/// The class containing all commands within the Utility module.
	/// </summary>

	public partial class UtilityCommands : DiscordModule
	{

		/// <summary>
		/// Holds all relevant settings and configuration for the Utility Commands Module.
		/// </summary>

		public UtilityConfiguration UtilityConfiguration { get; set; }

		/// <summary>
		/// Stores information regarding a user's birthday, usernames, nicknames, and other relevant data.
		/// </summary>

		public ProfilesDB ProfilesDB { get; set; }

		/// <summary>
		/// Stores information relevant to the Reminder system.
		/// </summary>

		public ReminderDB ReminderDB { get; set; }

		/// <summary>
		/// Stores relevant information about certain users' historical records.
		/// </summary>

		public UserRecords UserRecordsService { get; set; }

		/// <summary>
		/// Contains information relative to organic language management and time zones.
		/// </summary>

		public LanguageConfiguration LanguageConfiguration { get; set; }

		public IServiceProvider ServiceProvider { get; set; }

		/// <summary>
		/// Levelling role for getting the Awoo role.
		/// </summary>

		public LevelingConfiguration LevelingConfiguration { get; set; }


		/// <summary>
		/// Service responsible for parsing and overall managing interaction with commands issued by users.
		/// </summary>

		public CommandService CommandService { get; set; }

	}

}
