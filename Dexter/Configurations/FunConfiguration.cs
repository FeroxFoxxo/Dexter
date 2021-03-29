using Dexter.Abstractions;
using System.Collections.Generic;

namespace Dexter.Configurations {

    /// <summary>
    /// The FunConfiguration relates to attributes required by the FunCommands module.
    /// </summary>
    
    public class FunConfiguration : JSONConfig {

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
    }

}
