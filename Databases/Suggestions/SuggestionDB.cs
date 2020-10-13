using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Suggestions {
    public class SuggestionDB : EntityDatabase {
        public DbSet<Suggestion> Suggestions { get; set; }
    }
}
