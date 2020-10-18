using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.Configuration {
    public class Config {

        [Key]
        public string ConfigurationName { get; set; }

        public ConfigrationType ConfigurationType { get; set; }

    }
}
