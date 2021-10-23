using Dexter.Abstractions;

namespace Dexter.Configurations
{

	/// <summary>
	/// The BotConfiguration specifies global traits that the whole bot encompasses and requires.
	/// </summary>

	public class CustomCommandsConfiguration : JSONConfig
	{
		/// <summary>
		/// Whether to skip admin confirmation for staff users editing their staff command.
		/// </summary>

		public bool StaffCommandsSkipConfirmation { get; set; }


		/// <summary>
		/// The minimum patreon subscription tier required to have a personal custom command.
		/// </summary>

		public byte MinimumPatreonTierForCustomCommands { get; set; }

		/// <summary>
		/// The maximum length of a reply for any given custom command.
		/// </summary>
		public int MaximumReplyLength { get; set; }
	}
}