using System;
using Dexter.Configurations;
using Dexter.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Levels
{

    /// <summary>
    /// Holds relevant information about user XP for a given user
    /// </summary>

    public class UserLevel
    {

        /// <summary>
        /// The unique ID of the user this object represents and tracks
        /// </summary>

        [Key]

        public ulong UserID { get; set; }

        /// <summary>
        /// The total Voice XP of the user
        /// </summary>

        public long VoiceXP { get; set; } = 0;

        /// <summary>
        /// The total Text XP of the user 
        /// </summary>

        public long TextXP { get; set; } = 0;

        /// <summary>
        /// Calculates total level given a precalculated text level and voice level.
        /// </summary>
        /// <param name="config">The configuration file containing information about how to calculate total level.</param>
        /// <param name="tlvl">The user's text level</param>
        /// <param name="vlvl">The user's voice level</param>
        /// <returns>The total level of a user</returns>

        public int TotalLevel(LevelingConfiguration config, int tlvl = -1, int vlvl = -1)
        {
            if (tlvl < 0)
            {
                tlvl = config.GetLevelFromXP(TextXP, out _, out _);
            }
            if (vlvl < 0)
            {
                vlvl = config.GetLevelFromXP(VoiceXP, out _, out _);
            }

            if (!config.ManageTextXP) return vlvl;

            return config.LevelMergeMode switch
            {
                LevelMergeMode.AddSimple => tlvl + vlvl,
                LevelMergeMode.AddMerged => tlvl > vlvl ?
                    tlvl + (int)(vlvl * config.MergeFactor) :
                    vlvl + (int)(tlvl * config.MergeFactor),
                LevelMergeMode.AddXPSimple => config.GetLevelFromXP(
                    TextXP + VoiceXP,
                    out _, out _),
                _ => -1
            };
        }

        /// <summary>
        /// Gets the total XP of a user for the purpose of merged XP.
        /// </summary>
        /// <param name="config">The configuration file containing information about how to calculate total level.</param>
        /// <returns>The total XP of a user.</returns>

        public long TotalXP(LevelingConfiguration config)
        {
            return config.LevelMergeMode switch
            {
                LevelMergeMode.AddXPMerged => TextXP > VoiceXP ?
                    TextXP + (long)(VoiceXP * config.MergeFactor) :
                    VoiceXP + (long)(TextXP * config.MergeFactor),
                _ => TextXP + VoiceXP
            };
        }

        /// <summary>
        /// Obtains a text expression of the user's level and how it's calculated.
        /// </summary>
        /// <param name="config">The leveling configuration required to know how total level is calculated-</param>
        /// <param name="tlvl">The user's text level</param>
        /// <param name="vlvl">The user's voice level</param>
        /// <returns>Stringified expression of the user's total level calculation.</returns>

        public string TotalLevelStr(LevelingConfiguration config, int tlvl = -1, int vlvl = -1)
        {
            bool xpbased = config.LevelMergeMode is LevelMergeMode.AddXPMerged or LevelMergeMode.AddXPSimple;

            if (tlvl < 0 && !xpbased)
            {
                tlvl = config.GetLevelFromXP(TextXP, out _, out _);
            }
            if (vlvl < 0 && (!xpbased || !config.ManageTextXP))
            {
                vlvl = config.GetLevelFromXP(VoiceXP, out _, out _);
            }
            if (!config.ManageTextXP) return $"({vlvl} + 0)";

            int max = tlvl > vlvl ? tlvl : vlvl;
            int min = tlvl > vlvl ? vlvl : tlvl;

            switch (config.LevelMergeMode)
            {
                case LevelMergeMode.AddSimple:
                    return $"{tlvl} + {vlvl}";
                case LevelMergeMode.AddMerged:
                    return $"{max} + {(int)(min * config.MergeFactor)}";
                case LevelMergeMode.AddXPSimple:
                    return $"{(TextXP + VoiceXP).ToUnit()} XP";
                case LevelMergeMode.AddXPMerged:
                    long maxXP = TextXP > VoiceXP ? TextXP : VoiceXP;
                    long minXP = TextXP > VoiceXP ? VoiceXP : TextXP;
                    return $"{(maxXP + (long)(config.MergeFactor * minXP)).ToUnit()} XP";
                default:
                    return $"Unknown Level Merge Mode";
            }
        }

        /// <summary>
        /// The current level of the user
        /// </summary>

        public int TotalLevel(LevelingConfiguration config)
        {
            int tlvl = config.GetLevelFromXP(TextXP, out _, out _);
            int vlvl = config.GetLevelFromXP(VoiceXP, out _, out _);
            return TotalLevel(config, tlvl, vlvl);
        }

    }

}
