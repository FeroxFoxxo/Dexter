using Dexter.Abstractions;
using Dexter.Helpers;
using System.Collections.Generic;

namespace Dexter.Configurations {

    /// <summary>
    /// Holds relevant configuration for natural language processing and processing of strings.
    /// </summary>

    public class LanguageConfiguration : JSONConfig {

        /// <summary>
        /// Holds a list of plurals that can't be naturally processed through standard rules.
        /// </summary>

        public Dictionary<string, string> IrregularPlurals { get; set; }

        /// <summary>
        /// Dictates how many times a TermClass will try to produce new random terms if the newly produced one is already in its Cache.
        /// </summary>

        public uint TermRepetitionAversionFactor { get; set; }

        /// <summary>
        /// Character used to indicate the beginning of a term expression block.
        /// </summary>

        public char TermInsertionStartIndicator { get; set; }

        /// <summary>
        /// Character used to indicate the end of a term expression block.
        /// </summary>

        public char TermInsertionEndIndicator { get; set; }

        /// <summary>
        /// Error code returned by Language Functions when the provided arguments aren't formatted correctly or are incompatible with intended functions.
        /// </summary>

        public uint ErrorCodeInvalidArgument { get; set; }

        /// <summary>
        /// Correlates time zone abbreviations (such as EST) to their corresponding UTC offset.
        /// </summary>

        public SortedDictionary<string, TimeZoneData> TimeZones { get; set; }
    }

}
