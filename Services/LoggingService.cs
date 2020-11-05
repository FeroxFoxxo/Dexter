using Dexter.Abstractions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dexter.Services {
    /// <summary>
    /// The LoggingService is used to log messages from both the DiscordSocketClient and CommandService
    /// to the console and logging file for debugging purposes.
    /// </summary>
    public class LoggingService : InitializableModule {

        private readonly DiscordSocketClient Client;
        private readonly CommandService Commands;

        private string LogDirectory { get; }

        private readonly string LogFile;
        
        private static readonly object LockLogFile = new object();

        /// <summary>
        /// The constructor for the LoggingService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// This constructor also sets the LogDirectory to a folder called "logs" in the base directory, and the LogFile - a file with the current date in that directory.
        /// </summary>
        /// <param name="Client">The current instance of the DiscordSocketClient, which is used to hook into the Log delegate to run LogMessageAsync.</param>
        /// <param name="Commands">The CommandService is used to hook into the Log delegate to run LogMessageAsync.</param>
        public LoggingService(DiscordSocketClient Client, CommandService Commands) {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            LogFile = Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.log");
            this.Client = Client;
            this.Commands = Commands;
        }

        /// <summary>
        /// The AddDelegates override hooks into both the Commands.Log event and the Client.Log event to run LogMessageAsync.
        /// </summary>
        public override void AddDelegates() {
            Client.Log += LogMessageAsync;
            Commands.Log += LogMessageAsync;
        }

        /// <summary>
        /// The LogMessageAsync method creates the log file if it doesn't exist already, appends the logged message to the file,
        /// switches the console color to the severity of the message and finally logs the message to the console.
        /// </summary>
        /// <param name="LogMessage">The LogMessage field which gives us information about the message, for example the type of
        /// exception we have run into, the severity of the exception and the message of the exception to log.</param>
        /// <returns></returns>
        public Task LogMessageAsync(LogMessage LogMessage) {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            if (!File.Exists(GetLogFile()))
                File.Create(GetLogFile()).Dispose();

            string Date = DateTime.UtcNow.ToString("hh:mm:ss tt");

            string Severity = $"[{LogMessage.Severity}]";

            string Log = $"{Date} {Severity, 9} {LogMessage.Source}: {LogMessage.Exception?.ToString() ?? LogMessage.Message}";
            
            lock (LockLogFile)
                File.AppendAllText(GetLogFile(), Log + "\n");

            Console.ForegroundColor = LogMessage.Severity switch {
                LogSeverity.Info => ConsoleColor.Blue,
                LogSeverity.Critical => ConsoleColor.DarkRed,
                LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning => ConsoleColor.Yellow,
                LogSeverity.Verbose => ConsoleColor.Magenta,
                LogSeverity.Debug => ConsoleColor.DarkCyan,
                _ => ConsoleColor.Red,
            };

            return Console.Out.WriteLineAsync(Log);
        }

        /// <summary>
        /// Gets the LogFile from the instance of the class, initialized in the constructor.
        /// </summary>
        /// <returns>Returns the directory of which the log file resides in.</returns>
        public string GetLogFile() {
            return LogFile;
        }

    }
}
