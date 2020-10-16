using Dexter.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Dexter.Databases.CustomCommands {
    public class CustomCommandDB : EntityDatabase {

        public DbSet<CustomCommand> CustomCommands { get; set; }

        public CustomCommand GetCommandByNameOrAlias(string Name) {
            return CustomCommands.AsQueryable()
                        .Where(CustomCMD => CustomCMD.CommandName == Name || CustomCMD.Alias.Contains(Name))
                        .FirstOrDefault();
        }

    }
}
