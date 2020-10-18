using Dexter.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Dexter.Databases.Warnings {
    public class WarningsDB : EntityDatabase {
        public DbSet<Warning> Warnings { get; set; }

        public Warning[] GetWarnings(ulong UserID) =>
            Warnings.AsQueryable()
            .Where(Warning => Warning.User == UserID && Warning.Type != WarningType.Revolked)
            .ToArray();
    }
}
