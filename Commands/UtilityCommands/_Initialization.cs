using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord.Commands;
using System.Diagnostics;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands : ModuleBase<CommandModule> {

        private readonly CommandService CommandService;

        private readonly PerformanceCounter CPUCounter;

        private readonly PerformanceCounter MemCounter;

        private readonly BotConfiguration BotConfiguration;

        public UtilityCommands(CommandService _CommandService, BotConfiguration _BotConfiguration) {
            CommandService = _CommandService;
            BotConfiguration = _BotConfiguration;
            CPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            MemCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

    }
}
