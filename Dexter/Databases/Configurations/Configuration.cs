using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Configurations {

    /// <summary>
    /// The Configuration class contains information on a module, such as its name and configuration type.
    /// </summary>
    
    public class Configuration {

        /// <summary>
        /// The ConfigurationName is the KEY of the table.
        /// It is the sanitized name of the module that the configuration is attached to.
        /// </summary>
        
        [Key]

        public string ConfigurationName { get; set; }

        /// <summary>
        /// The ConfigurationType specifies what state the module is in.
        /// It specifies whether the module is enabled or disabled, or if it is essential to the bot.
        /// </summary>
        
        public ConfigurationType ConfigurationType { get; set; }

    }

}
