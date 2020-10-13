using Dexter.Abstractions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dexter.Services {
    public class LoggingService : InitializableModule {

        private readonly DiscordSocketClient Client;
        private readonly CommandService Commands;

        public string LogDirectory { get; }
        public string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.log");

        private static readonly object LockLogFile = new object();

        public LoggingService(DiscordSocketClient _Client, CommandService _Commands) {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Client = _Client;
            Commands = _Commands;
        }

        public override void AddDelegates() {
            Client.Log += LogMessageAsync;
            Commands.Log += LogMessageAsync;
        }

        public Task LogMessageAsync(LogMessage Message) {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            if (!File.Exists(LogFile))
                File.Create(LogFile).Dispose();

            string Date = DateTime.UtcNow.ToString("hh:mm:ss tt");

            string Severity = "[" + Message.Severity + "]";

            string Log = $"{Date} {Severity,9} {Message.Source}: {Message.Exception?.ToString() ?? Message.Message}";
            
            lock (LockLogFile)
                File.AppendAllText(LogFile, Log + "\n");

            return Console.Out.WriteLineAsync(Log);
        }

    }
}
