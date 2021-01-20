using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.ReactionMenus {

    /// <summary>
    /// Represents a menu that can be navigated through the use of message reactions.
    /// </summary>

    public class ReactionMenu {

        /// <summary>
        /// The unique numerical ID of the target message which contains the menu.
        /// </summary>

        [Key]
        public ulong MessageID { get; set; }

        /// <summary>
        /// The value of the current page the menu is displaying.
        /// </summary>

        public int CurrentPage { get; set; }

        /// <summary>
        /// The integer identifier of the Embed Menu information this ReactionMenu displays.
        /// </summary>

        public int EmbedMenuIndex { get; set; }

        /// <summary>
        /// The integer identifier of the Color scheme information this ReactionMenu displays.
        /// </summary>

        public int ColorMenuIndex { get; set; }

    }

}
