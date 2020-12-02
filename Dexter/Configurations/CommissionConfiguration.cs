using Dexter.Abstractions;

namespace Dexter.Configurations {
    
    public class CommissionCooldownConfiguration : JSONConfiguration {

        public ulong CommissionsCornerID { get; set; }

        public long CommissionCornerCooldown { get; set; }

        public long GracePeriod { get; set; }

    }

}
