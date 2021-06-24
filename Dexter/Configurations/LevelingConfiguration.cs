using Dexter.Abstractions;
using System;
using System.Collections.Generic;

namespace Dexter.Configurations {

    public class LevelingConfiguration : JSONConfig {

        /// <summary>
        /// The interval between attempts to give users experience, in seconds.
        /// </summary>

        public int XPIncrementTime { get; set; }

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
        /// The text channel where VC level ups should be announced.
        /// </summary>

        public ulong VoiceTextChannel { get; set; }

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
        /// Indicates which guild ID to use for mee6 XP synchronization for the <see cref="Dexter.Commands.LevelingCommands.LoadLevelsFromMee6Command(int, int, string)"/> method.
        /// </summary>

        public ulong Mee6SyncGuildId { get; set; }

        /// <summary>
        /// Returns the amount of XP required for a given level
        /// </summary>
        /// <param name="level">The target level</param>
        /// <returns>The XP required to reach a given <paramref name="level"/>.</returns>

        public int GetXPForLevel(double level) {
            if (level < 0) { return 0; }

            double xp = 0;
            for (int i = 0; i < DexterXPCoefficients.Length; i++) {
                xp += DexterXPCoefficients[i] * Math.Pow(level, i);
            }
            return (int)xp;
        }

        /// <summary>
        /// Gets the level of a user given the amount of XP they have.
        /// </summary>
        /// <param name="xp">The total XP accrued by the user.</param>
        /// <param name="residualXP">The XP accrued since the last level up.</param>
        /// <param name="levelXP">The total size of the range of XP required for the obtained level.</param>
        /// <param name="throwsError">Whether to throw an error if the operation takes too long, if <see langword="false"/>, the method will return the best approximation it has.</param>
        /// <returns>The level of the user, ignoring residual XP.</returns>

        public int GetLevelFromXP(long xp, out long residualXP, out long levelXP, bool throwsError = false) {
            //solve [config.DexterXPCoefficients] [1, x, x^2, x^3 ... x^n]t = xp
            //through Newton's Method
            double level = 100;

            int attempts = 0;

            long lowerXP = GetXPForLevel((int)level);
            long upperXP = GetXPForLevel((int)level + 1);
            while (xp < lowerXP || xp >= upperXP) {
                level = ApproximateLevel(xp, lowerXP, level);
                lowerXP = GetXPForLevel((int)level);
                upperXP = GetXPForLevel((int)level + 1);
                if (attempts++ > 150) {
                    if (throwsError)
                        throw new TimeoutException("The user level calculation took too long!");
                    else {
                        residualXP = -1;
                        levelXP = upperXP;
                        return (int)level;
                    }
                }
            }

            residualXP = xp - lowerXP;
            levelXP = upperXP - lowerXP;
            return (int)level;
        }

        private double ApproximateLevel(long xp, double guess) {
            return ApproximateLevel(xp, GetXPForLevel(guess));
        }

        private double ApproximateLevel(long xp, long lowerLevelXP, double guess) {
            return (xp - lowerLevelXP) / GetDerivativeAtLevel(guess) + guess;
        }

        private double GetDerivativeAtLevel(double level) {
            double d = 0;
            for (int i = 1; i < DexterXPCoefficients.Length; i++) {
                d += i * DexterXPCoefficients[i] * Math.Pow(level, i - 1);
            }
            return d;
        }
    }

    /// <summary>
    /// Dictates how the total level is calculated based on the text and voice levels of a user.
    /// </summary>

    public enum LevelMergeMode {
        /// <summary>
        /// Total level = Maximum level + Minimum level
        /// </summary>
        AddSimple,
        /// <summary>
        /// Total level = Maximum level + Minimum level * MergeFactor
        /// </summary>
        AddMerged
    }
}
