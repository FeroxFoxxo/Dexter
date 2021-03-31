using Dexter.Abstractions;

namespace Dexter.Configurations {

    /// <summary>
    /// Configures the relevant aspects of the Utility Commands Module.
    /// </summary>

    public class UtilityConfiguration : JSONConfig {

        /// <summary>
        /// The maximum number of items that can appear on an Embed Menu's page for upcoming reminders.
        /// </summary>

        public int ReminderMaxItemsPerPage { get; set; }

        /// <summary>
        /// The maximum length of a reminder that items in an embedMenu will appear with.
        /// </summary>

        public int ReminderMaxCharactersPerItem { get; set; }

        public string WolframAppAPI { get; set; }

    }
}
