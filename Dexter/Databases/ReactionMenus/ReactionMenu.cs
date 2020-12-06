using Discord;

namespace Dexter.Databases.ReactionMenus {

    public class ReactionMenu {

        public ulong MessageID { get; set; }

        public int CurrentPage { get; set; }

        public EmbedBuilder[] EmbedMenus { get; set; }

    }

}
