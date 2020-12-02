using Discord;

namespace Dexter.Abstractions {

    public class ReactionMenu {

        public ulong MessageID { get; set; }

        public int CurrentPage { get; set; }

        public EmbedBuilder[] EmbedMenus { get; set; }

    }

}
