using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.ReactionMenus {

    public class ReactionMenu {

        [Key]
        public ulong MessageID { get; set; }

        public int CurrentPage { get; set; }

        public int EmbedMenuIndex { get; set; }

        public int ColorMenuIndex { get; set; }

    }

}
