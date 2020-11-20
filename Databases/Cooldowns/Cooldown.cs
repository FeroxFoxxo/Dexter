using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Cooldowns {
    
    public class Cooldown {
        
        [Key]
        public string Identifier { get; set; }

        public long TimeOfCooldown { get; set; }

    }

}
