using Dexter.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Configuration {
    public class ConfigurationDB : EntityDatabase {

        public DbSet<Config> Configurations { get; set; }

    }
}
