using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Levels {

    public class UserLevel {

        [Key]

        public ulong UserID { get; set; }

        public int CurrentUserLevel { get; set; }

        public long UserXP { get; set; }

    }

}
