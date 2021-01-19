using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.ReactionMenus {

    /// <summary>
    /// Creates and manages a database for storing information about ReactionMenus.
    /// </summary>

    public class ReactionMenuDB : Database {

        /// <summary>
        /// Holds core information about an active ReactionMenu, such as CurrentPage and message IDs.
        /// </summary>

        public DbSet<ReactionMenu> ReactionMenus { get; set; }

        /// <summary>
        /// Holds template embeds for the possible ReactionMenus, which are referenced when displaying pages.
        /// </summary>

        public DbSet<EmbedMenu> EmbedMenus { get; set; }

        /// <summary>
        /// Holds template color schemes for the possible ReactionMenu embeds, which are referenced when displaying pages.
        /// </summary>

        public DbSet<ColorMenu> ColorMenus { get; set; }

    }

}
