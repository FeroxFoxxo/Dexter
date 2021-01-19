using Dexter.Configurations;

namespace Dexter.Abstractions {

    /// <summary>
    /// Encapsulates necessary components to deal with command management.
    /// </summary>

    public class HelpAbstraction {

        /// <summary>
        /// Serves as an interface between global bot configuration settings and its commands.
        /// </summary>

        public BotConfiguration BotConfiguration { get; set; }

    }

}
