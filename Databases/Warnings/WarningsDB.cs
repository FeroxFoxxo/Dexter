using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Warnings {
    public class WarningsDB : EntityDatabase {
        public DbSet<Warning> Warnings { get; set; }
    }
}
