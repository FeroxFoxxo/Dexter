using Dexter.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Suggestions {
    public class SuggestionDB : EntityDatabase {
        public DbSet<Suggestion> Suggestions { get; set; }
    }

    public class Suggestion {
        [Key]
        public string TrackerID { get; set; }

        public string Suggestor { get; set; }
        public string Status { get; set; }
        public string Content { get; set; }
        public string MessageID { get; set; }
        public string StaffMessageID { get; set; }
        public string Expiry { get; set; }
    }
}
