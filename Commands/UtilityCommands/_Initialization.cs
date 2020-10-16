using Dexter.Configuration;
using Dexter.Core.Abstractions;
using Dexter.Services;
using Discord.Commands;
using System.Diagnostics;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands : ModuleD {

        private readonly CommandService CommandService;

        private readonly PerformanceCounter CPUCounter;

        private readonly PerformanceCounter MemCounter;

        private readonly LoggingService LoggingService;

        private readonly BotConfiguration BotConfiguration;

        public UtilityCommands(CommandService _CommandService, BotConfiguration _BotConfiguration, LoggingService _LoggingService) {
            CommandService = _CommandService;
            BotConfiguration = _BotConfiguration;
            LoggingService = _LoggingService;
            CPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            MemCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

    }
}
