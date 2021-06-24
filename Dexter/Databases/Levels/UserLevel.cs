using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dexter.Configurations;
using Newtonsoft.Json;

namespace Dexter.Databases.Levels {

    /// <summary>
    /// Holds relevant information about user XP for a given user
    /// </summary>

    public class UserLevel {

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

        public static int TotalLevel(LevelingConfiguration config, int tlvl, int vlvl) {
            if (!config.ManageTextXP) return vlvl;
            
            return config.LevelMergeMode switch {
                LevelMergeMode.AddSimple => tlvl + vlvl,
                LevelMergeMode.AddMerged => tlvl > vlvl ?
                    tlvl + (int)(vlvl * config.MergeFactor) :
                    vlvl + (int)(tlvl * config.MergeFactor),
                _ => -1,
            };
        }



        /// <summary>
        /// Obtains a text expression of the user's level and how it's calculated.
        /// </summary>
        /// <param name="config">The leveling configuration required to know how total level is calculated-</param>
        /// <param name="tlvl">The user's text level</param>
        /// <param name="vlvl">The user's voice level</param>
        /// <returns>Stringified expression of the user's total level calculation.</returns>

        public static string TotalLevelStr(LevelingConfiguration config, int tlvl, int vlvl) {
            if (!config.ManageTextXP) return $"({vlvl} + 0)";

            switch (config.LevelMergeMode) {
                case LevelMergeMode.AddSimple:
                    return $"{tlvl} + {vlvl}";
                case LevelMergeMode.AddMerged:
                    int max = tlvl > vlvl ? tlvl : vlvl;
                    int min = tlvl > vlvl ? vlvl : tlvl;
                    return $"{max} + {(int)(min * config.MergeFactor)}";
                default:
                    return $"Unknown Level Merge Mode";
            }
        }

        /// <summary>
        /// The current level of the user
        /// </summary>

        public int TotalLevel(LevelingConfiguration config) {
            int tlvl = config.GetLevelFromXP(TextXP, out _, out _);
            int vlvl = config.GetLevelFromXP(VoiceXP, out _, out _);
            return TotalLevel(config, tlvl, vlvl);
        }

        /// <summary>
        /// Stringifies the object
        /// </summary>
        /// <returns>A string expression of the object detailing the ID and the XP levels</returns>

        public override string ToString() {
            return $"{UserID} - Text: {TextXP} - Voice: {VoiceXP}";
        }
    }

    /// <summary>
    /// Extra user-specific data on how they wish to have their level displayed.
    /// </summary>

    [Serializable]
    public class LevelPreferences {

        /// <summary>
        /// The unique identifier of the user this object corresponds to.
        /// </summary>

        [Key]
        public ulong UserId { get; set; }

        /// <summary>
        /// The color to display the XP in expressed as a raw RGB value.
        /// </summary>

        public ulong XpColor { get; set; } = 0xff70cefe;

        /// <summary>
        /// The background image for the rank card.
        /// </summary>

        public string Background { get; set; } = "default";

        /// <summary>
        /// Whether to render a circular border around the user's profile picture.
        /// </summary>

        public bool PfpBorder { get; set; } = true;

        /// <summary>
        /// Whether to crop the profile picture into a circle.
        /// </summary>

        public bool CropPfp { get; set; } = true;
    }

}
