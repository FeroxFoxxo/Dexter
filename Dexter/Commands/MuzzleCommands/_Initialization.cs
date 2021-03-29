using Dexter.Abstractions;
using Dexter.Configurations;
using System;

namespace Dexter.Commands {

    /// <summary>
    /// The class containing all commands withing the Muzzle module.
    /// </summary>

    public partial class MuzzleCommands : DiscordModule {

        /// <summary>
        /// Works as an interface between the configuration files attached to the Muzzle module and its commands.
        /// </summary>

        public MuzzleConfiguration MuzzleConfiguration { get; set; }

        /// <summary>
        /// Serves for dependency injection of a pseudo-random number generator in commands within the Muzzle module.
        /// </summary>

        public Random Random { get; set; }

    }

}
