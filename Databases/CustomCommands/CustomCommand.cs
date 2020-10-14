using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.CustomCommands {
    public class CustomCommand {

        [Key]
        public string CommandName { get; set; }

        public string Reply { get; set; }

        public string Alias { get; set; }

    }
}
