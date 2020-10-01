using Dexter.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases {
    public class SuggestionDB : EntityDatabase {
        public DbSet<Suggestion> Suggestions { get; set; }
    }

    public class Suggestion {
        [Key]
        public string TrackerID { get; set; }

        public ulong Suggestor { get; set; }
        public SuggestionStatus Status { get; set; }
        public string Content { get; set; }
        public ulong MessageID { get; set; }
        public ulong StaffMessageID { get; set; }
        public string Expiry { get; set; }
    }

    public enum SuggestionStatus {
        Suggested,
        Pending,
        Approved,
        Declined
    }
}
