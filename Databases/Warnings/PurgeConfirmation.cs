using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Warnings {
    public class PurgeConfirmation {

        [Key]
        public string Token { get; set; }

        public ulong User { get; set; }

    }
}
