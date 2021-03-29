using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Cooldowns {
    
    /// <summary>
    /// Class working as a global interface for all timers and cooldowns.
    /// </summary>

    public class Cooldown {
        
        /// <summary>
        /// A unique identifier for the cooldown object.
        /// </summary>

        [Key]
        public string Token { get; set; }

        /// <summary>
        /// The UNIX time the cooldown was first issued, in seconds.
        /// </summary>

        public long TimeOfCooldown { get; set; }

    }

}
