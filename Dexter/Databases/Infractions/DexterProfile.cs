using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Infractions {
    
    public class DexterProfile {

        [Key]
        public ulong UserID { get; set; }

        public short InfractionAmount { get; set; }

        public string CurrentMute { get; set; }

        public string CurrentPointTimer { get; set; }

    }

}
