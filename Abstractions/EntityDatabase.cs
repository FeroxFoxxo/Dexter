using Microsoft.EntityFrameworkCore;

namespace Dexter.Abstractions {
    public class EntityDatabase : DbContext {
        protected override void OnConfiguring(DbContextOptionsBuilder Options) => Options.UseSqlite($"Data Source={GetType().Name}.db");
    }
}
