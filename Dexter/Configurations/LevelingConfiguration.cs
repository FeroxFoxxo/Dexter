using System;
using System.Collections.Generic;
using Dexter.Abstractions;

namespace Dexter.Configurations
{

	/// <summary>
	/// The configuration object abstracting all necessary settings and customizable items for the Leveling module.
	/// </summary>

	public class LevelingConfiguration : JSONConfig
	{

		/// <summary>
		/// The interval between attempts to give users experience, in seconds.
		/// </summary>

		public int XPIncrementTime { get; set; }

		/// <summary>
		/// Conditions whether roles will be modified by Dexter at all in the process of processing experience.
		/// </summary>

		public bool HandleRoles { get; set; }

		/// <summary>
		/// Minimum amount of users in a voice channel in order to obtain XP.
		/// </summary>

		public int VCMinUsers { get; set; }

		/// <summary>
		/// Voice channels where the user won't get XP.
		/// </summary>

		public ulong[] DisabledVCs { get; set; }

		/// <summary>
		/// The lower end of the range of possible XP to obtain per minute in VCs
		/// </summary>

		public int VCMinXPGiven { get; set; }

		/// <summary>
		/// The upper end of the range of possible XP to obtain per minute in VCs
		/// </summary>

		public int VCMaxXPGiven { get; set; }

		/// <summary>
		/// Relates the levels at which roles are obtained with which roles have to be added to the user.
		/// </summary>

		public Dictionary<int, ulong> Levels { get; set; }

		/// <summary>
		/// The ID of the basic member role.
		/// </summary>

		public ulong MemberRoleID { get; set; }

		/// <summary>
		/// The level at which to award the basic member role; disabled if negative.
		/// </summary>

		public int MemberRoleLevel { get; set; }

		/// <summary>
		/// The text channel where VC level ups should be announced.
		/// </summary>

		public ulong VoiceTextChannel { get; set; }

		/// <summary>
		/// Whether to send a level up message in the configured <see cref="VoiceTextChannel"/> when a user gets a voice level.
		/// </summary>

		public bool VoiceSendLevelUpMessage { get; set; }

		/// <summary>
		/// Whether to count muted users for the purpose of measuring whether a voice channel is active.
		/// </summary>

		public bool VoiceCountMutedMembers { get; set; }

		/// <summary>
		/// The guild containing the text channel used for image dumps.
		/// </summary>

		public ulong CustomImageDumpsGuild { get; set; }

		/// <summary>
		/// The text channel where custom background images for rank cards will be dumped.
		/// </summary>

		public ulong CustomImageDumpsChannel { get; set; }

		/// <summary>
		/// The maximum allowed size for an custom background image.
		/// </summary>

		public int CustomImageSizeLimit { get; set; }

		/// <summary>
		/// The minimum required level to be able to set a custom image as your profile picture.
		/// </summary>

		public int CustomImageMinimumLevel { get; set; }

		/// <summary>
		/// A list of text channels where XP is disabled.
		/// </summary>

		public ulong[] DisabledTCs { get; set; }

		/// <summary>
		/// Whether Dexter should manage XP from text messages at all.
		/// </summary>

		public bool ManageTextXP { get; set; }

		/// <summary>
		/// Minimum range of XP given randomly (uniform) per text message every <see cref="XPIncrementTime"/>
		/// </summary>

		public int TextMinXPGiven { get; set; }

		/// <summary>
		/// Maximum range of XP given randomly (uniform) per text message every <see cref="XPIncrementTime"/>
		/// </summary>

		public int TextMaxXPGiven { get; set; }

		/// <summary>
		/// Whether to send a level up message when a user levels up through text.
		/// </summary>

		public bool TextSendLevelUpMessage { get; set; }

		/// <summary>
		/// The coefficients of XP required to reach a given level 'x', where the index of each item equals the degree of its factor.
		/// </summary>

		public float[] DexterXPCoefficients { get; set; }

		/// <summary>
		/// Dictates how total level is calculated from voice level and text level.
		/// </summary>

		public LevelMergeMode LevelMergeMode { get; set; }

		/// <summary>
		/// Conditions certain modes of operation specified in LevelMergeMode
		/// </summary>

		public float MergeFactor { get; set; }

		/// <summary>
		/// The message to send when a user levels up. Use {MENTION} to include a mention; {LVL} to include the level they advanced to, and {TYPE} to include the XP subsystem used. 
		/// </summary>

		public string LevelUpMessage { get; set; }

		/// <summary>
		/// Maximum number of users to display in a leaderboard.
		/// </summary>

		public int MaxLeaderboardItems { get; set; }

		/// <summary>
		/// Indicates which guild ID to use for mee6 XP synchronization for the <see cref="Commands.LevelingCommands.LoadLevelsFromMee6Command(int, int, string)"/> method.
		/// </summary>

		public ulong Mee6SyncGuildId { get; set; }

		/// <summary>
		/// The leveling replacement role that removes a user's ability to change their nickname.
		/// </summary>

		public ulong NicknameDisabledRole { get; set; }

		/// <summary>
		/// The unique ID of the role that grants the ability to change one's nickname that must be replaced by the <see cref="NicknameDisabledRole"/>.
		/// </summary>

		public ulong NicknameDisabledReplacement { get; set; }

		/// <summary>
		/// Returns the amount of XP required for a given level
		/// </summary>
		/// <param name="level">The target level</param>
		/// <returns>The XP required to reach a given <paramref name="level"/>.</returns>

		public long GetXPForLevel(double level)
		{
			return (long)GetXPForLevelFull(level);
		}

		private double GetXPForLevelFull(double level)
		{
			if (level < 0) { return 0; }

			double xp = 0;
			for (int i = 0; i < DexterXPCoefficients.Length; i++)
			{
				xp += DexterXPCoefficients[i] * Math.Pow(level, i);
			}
			return xp;
		}

		/// <summary>
		/// Gets the level of a user given the amount of XP they have.
		/// </summary>
		/// <param name="xp">The total XP accrued by the user.</param>
		/// <param name="residualXP">The XP accrued since the last level up.</param>
		/// <param name="levelXP">The total size of the range of XP required for the obtained level.</param>
		/// 
		/// <returns>The level of the user, ignoring residual XP.</returns>

		public int GetLevelFromXP(long xp, out long residualXP, out long levelXP)
		{
			//solve [config.DexterXPCoefficients] [1, x, x^2, x^3 ... x^n]t = xp
			//through binary approximation
			int minlevel = 0;
			int maxlevel = 100;
			while (xp > GetXPForLevel(maxlevel))
			{
				minlevel = maxlevel;
				maxlevel *= 2;
			}

			int level = ApproximateLevel(xp, ref minlevel, ref maxlevel, out long lowerXP, out long upperXP);

			residualXP = xp - lowerXP;
			levelXP = upperXP - lowerXP;
			return level;
		}

		private int ApproximateLevel(long xp, ref int lowerbound, ref int upperbound, out long lvlxp, out long nextlvlxp)
		{
			int attempts = 0;
			while (attempts++ < 500)
			{
				int middle = (lowerbound + upperbound) / 2;

				long xpmiddle = GetXPForLevel(middle);
				long xpmaxmiddle = GetXPForLevel(middle + 1);

				if (xp >= xpmaxmiddle)
				{
					lowerbound = middle + 1;
				}
				else if (xp < xpmiddle)
				{
					upperbound = middle;
				}
				else
				{
					lvlxp = xpmiddle;
					nextlvlxp = xpmaxmiddle;
					return middle;
				}
			}
			throw new Exception($"Unable to calculate level for XP {xp}, reached bounds {lowerbound}-{upperbound}");
		}
	}

	/// <summary>
	/// Dictates how the total level is calculated based on the text and voice levels of a user.
	/// </summary>

	public enum LevelMergeMode
	{
		/// <summary>
		/// Total level = Maximum level + Minimum level
		/// </summary>
		AddSimple,
		/// <summary>
		/// Total level = Maximum level + Minimum level * MergeFactor
		/// </summary>
		AddMerged,
		/// <summary>
		/// Total level = Level(MaxLevel.xp + MinLevel.xp)
		/// </summary>
		AddXPSimple,
		/// <summary>
		/// Total level = Level(MaxLevel.xp + MinLevel.xp * MergeFactor)
		/// </summary>
		AddXPMerged
	}
}
