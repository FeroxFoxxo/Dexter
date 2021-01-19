using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.ReactionMenus {

    /// <summary>
    /// This class abstracts and serves to store the color information of a ReactionMenu's embeds.
    /// </summary>

    public class ColorMenu {

        /// <summary>
        /// The unique identifier of a color scheme for any given ReactionMenu.
        /// </summary>

        [Key]
        public int ColorIndex { get; set; }

        /// <summary>
        /// The JSON string parsable as a <c>uint[]</c> object for the color information for any given ReactionMenu.
        /// </summary>

        public string ColorMenuJSON { get; set; }

    }

}
