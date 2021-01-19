using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.ReactionMenus {

    /// <summary>
    /// Represents a template for the embeds displayed in a ReactionMenu.
    /// </summary>

    public class EmbedMenu {

        /// <summary>
        /// Unique identifier of the EmbedMenu template.
        /// </summary>

        [Key]
        public int EmbedIndex { get; set; }

        /// <summary>
        /// The JSON string parsable as an <c>EmbedBuilder[]</c> object for the embed information for any given ReactionMenu.
        /// </summary>

        public string EmbedMenuJSON { get; set; }

    }

}
