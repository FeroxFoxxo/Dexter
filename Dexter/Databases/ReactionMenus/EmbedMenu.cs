using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.ReactionMenus {

    public class EmbedMenu {

        [Key]
        public int EmbedIndex { get; set; }

        public string EmbedMenuJSON { get; set; }

    }

}
