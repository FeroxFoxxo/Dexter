using System.Collections.Generic;
using Dexter.Abstractions;

namespace Dexter.Configurations
{

	/// <summary>
	/// The FunConfiguration relates to attributes required by the FunCommands module.
	/// </summary>

	public class FunConfiguration : JSONConfig
	{

		/// <summary>
		/// The EMOJI GUILD ID field is the snowflake ID of the server in which the eight-ball and gay emojis are stored.
		/// </summary>

		public ulong EmojiGuildID { get; set; }

		/// <summary>
		/// The EMOJI ID field is a dictionary of the type of emoji (EG love, annoyed, wut) and their corresponding emoji IDs.
		/// </summary>

		public Dictionary<string, ulong> EmojiIDs { get; set; }

		/// <summary>
		/// The EIGHT BALL field specifies the responces the eight-ball command can give.
		/// </summary>

		public Dictionary<string, string[]> EightBall { get; set; }

		/// <summary>
		/// The EIGHT BALL EMOJI field links the type of responce the eight-ball command gives to its corresponding emoji in the EMOJI IDs.
		/// </summary>

		public Dictionary<string, string> EightBallEmoji { get; set; }

		/// <summary>
		/// The PATH to the directory containing the resources for building headpat emoji.
		/// </summary>

		public string HeadpatsDir { get; set; }

		/// <summary>
		/// <para>The matrix of positions of the profile picture when altered by the headpat animation.</para>
		/// <para>Each subarray should have four elements: x, y, width, and height respectively; and corresponds to one frame.</para>
		/// </summary>

		public List<List<ushort>> HeadpatPositions { get; set; }

		/// <summary>
		/// The ID of the Guild (server) to temporarily store created headpat emoji.
		/// </summary>

		public ulong HeadpatStorageGuild { get; set; }

		/// <summary>
		/// A descriptive name for the webHook temporarily created in a channel to display the headpat emoji.
		/// </summary>

		public string HeadpatWebhookName { get; set; }

		/// <summary>
		/// A list of generic openings to a writing prompt, something like "once upon a time,".
		/// </summary>

		public List<string> WritingPromptOpenings { get; set; }

		/// <summary>
		/// A string-string dictionary containing definitions for term classes which will be used as keys later on in order to randomly generate writing prompts.
		/// </summary>

		public Dictionary<string, string[]> WritingPromptTerms { get; set; }

		/// <summary>
		/// A list of writing prompts phrased after an opening, a few keywords can be used to generate random terms.
		/// Using the keywords "{NOUNX}", "{NAMEX}", or "{ADJECTIVEX}" where 'X' is a number will replace them with a randomly generated term of that category.
		/// </summary>

		public List<string> WritingPromptPredicates { get; set; }

		/// <summary>
		/// The maximum allowed number of coin flips using the coinflip command
		/// </summary>

		public int MaxCoinFlips { get; set; }

		/// <summary>
		/// The unique channel ID of the channels designated for the games submodule.
		/// </summary>

		public ulong[] GamesChannels { get; set; }

		/// <summary>
		/// The unique channel IDs of the channels wherein GameChannelRestricted methods should not be possible to run.
		/// </summary>

		public ulong[] GamesOnlyChannels { get; set; }

		/// <summary>
		/// Sets the lives a default game of hangman is set to on reset and creation.
		/// </summary>

		public int HangmanDefaultLives { get; set; }

		/// <summary>
		/// The unique numerical ID of the image dumps channel for the Games module.
		/// </summary>

		public ulong GamesImageDumpsChannel { get; set; }

		/// <summary>
		/// An array of valid chess theme names.
		/// </summary>

		public string[] ChessThemes { get; set; }

		/// <summary>
		/// Gets a set of custom positions that can be set in chess instead of a position in FEN notation.
		/// </summary>

		public Dictionary<string, string> ChessPositions { get; set; }

		/// <summary>
		/// The maximum amount of additional rolls that can be performed due to die explosions when rolling.
		/// </summary>

		public int MaxDieRollExplosions { get; set; }

		/// <summary>
		/// The maximum number of dice that can be rolled at once.
		/// </summary>

		public int MaxDieRolls { get; set; }

		/// <summary>
		/// The maximum length of an individual roll expression.
		/// </summary>

		public int MaxDieRollExpressionLength { get; set; }

		/// <summary>
		/// The maximum number of roll expressions that can be printed due to roll modifiers applied to the roll.
		/// </summary>

		public int MaxDieRollExpressionCount { get; set; }

		/// <summary>
		/// A role that denied roleplaying commands being run on a person.
		/// </summary>

		public ulong RpDeniedRole { get; set; }
    }

}
