using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.ReactionMenus {

    public class ReactionMenuDB : Database {

        public DbSet<ReactionMenu> ReactionMenus { get; set; }

        public DbSet<EmbedMenu> EmbedMenus { get; set; }

        public DbSet<ColorMenu> ColorMenus { get; set; }

    }

}
