namespace Dexter.Enums
{
	/// <summary>
	/// The EmbedCallingType specifies whether an embed is called from
	/// a game, service or command to remove itself from the footer
	/// of the embed (cleanup).
	/// </summary>

	public enum EmbedCallingType
	{

		/// <summary>
		/// Embeds called from the Game Template class will enact the Game type
		/// eg GameHangman -> Hangman
		/// </summary>
		Game,

		/// <summary>
		/// Embeds called from the Service class with enact the Service type.
		/// eg StartupService -> Startup
		/// </summary>
		Service,

		/// <summary>
		/// Embeds called from the DiscordModule and related classes will enact the Command type.
		/// eg FunCommands -> Fun
		/// </summary>
		Command

	}

}
