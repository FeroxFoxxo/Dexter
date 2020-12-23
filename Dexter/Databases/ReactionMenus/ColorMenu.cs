using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.ReactionMenus {

    public class ColorMenu {

        [Key]
        public int ColorIndex { get; set; }

        public string ColorMenuJSON { get; set; }

    }

}
