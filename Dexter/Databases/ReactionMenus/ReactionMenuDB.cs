using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.ReactionMenus {

    public class ReactionMenuDB : Database {

        public DbSet<ReactionMenu> ReactionMenus { get; set; }

    }

}
