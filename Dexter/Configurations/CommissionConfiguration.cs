using Dexter.Abstractions;

namespace Dexter.Configurations {
    
    public class CommissionCooldownConfiguration : JSONConfig {

        public ulong CommissionsCornerID { get; set; }

        public long CommissionCornerCooldown { get; set; }

        public long GracePeriod { get; set; }

    }

}
