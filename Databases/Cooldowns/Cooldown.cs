using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Cooldowns {
    
    public class Cooldown {
        
        [Key]
        public string Token { get; set; }

        public long TimeOfCooldown { get; set; }

    }

}
