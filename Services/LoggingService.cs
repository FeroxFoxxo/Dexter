using Dexter.Abstractions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Dexter.Services {

    /// <summary>
    /// The LoggingService is used to log messages from both the DiscordSocketClient and CommandService
    /// to the console and logging file for debugging purposes.
    /// </summary>
    public class LoggingService : InitializableModule {

        private readonly DiscordSocketClient DiscordSocketClient;

        private readonly CommandService CommandService;

        private string LogDirectory { get; }

        private readonly string LogFile;
        
        private readonly object LockLogFile;

        private bool LockedCMDOut;

        private readonly List<LogMessage> BackloggedMessages;

        /// <summary>
        /// The constructor for the LoggingService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// This constructor also sets the LogDirectory to a folder called "logs" in the base directory, and the LogFile - a file with the current date in that directory.
        /// </summary>
        /// <param name="DiscordSocketClient">The current instance of the DiscordSocketClient, which is used to hook into the Log delegate to run LogMessageAsync.</param>
        /// <param name="CommandService">The CommandService is used to hook into the Log delegate to run LogMessageAsync.</param>
        public LoggingService(DiscordSocketClient DiscordSocketClient, CommandService CommandService) {
            this.DiscordSocketClient = DiscordSocketClient;
            this.CommandService = CommandService;

            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            LogFile = Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.log");

            LockLogFile = new object();
            LockedCMDOut = false;

            BackloggedMessages = new List<LogMessage>();
        }

        /// <summary>
        /// The AddDelegates override hooks into both the Commands.Log event and the Client.Log event to run LogMessageAsync.
        /// </summary>
        public override void AddDelegates() {
            DiscordSocketClient.Log += LogMessageAsync;
            CommandService.Log += LogMessageAsync;
        }

        /// <summary>
        /// The TryLogMessage method sees if the command output is locked and, if so, adds it to a list of backlogged messages.
        /// If it is not blocked it simply outputs the message to the console, running through any previously blocked messages
        /// </summary>
        /// <param name="LogMessage">The LogMessage field which gives us information about the message, for example the type of
        /// exception we have run into, the severity of the exception and the message of the exception to log.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task LogMessageAsync(LogMessage LogMessage) {
            if (LockedCMDOut) {
                await LogToConsole(LogMessage);
                BackloggedMessages.Add(LogMessage);
                return;
            }

            if (BackloggedMessages.Count > 0) {
                foreach (LogMessage Message in BackloggedMessages)
                    await LogToFile(Message);

                BackloggedMessages.Clear();
            }

            await LogToFile(LogMessage);
        }

        /// <summary>
        /// The CreateLog method creates the log string from an instance of LogMessage and returns it.
        /// </summary>
        /// <param name="LogMessage">The LogMessage field which gives us information about the message, for example the type of
        /// exception we have run into, the severity of the exception and the message of the exception to log.</param>
        /// <returns>A string of the logged message.</returns>
        private static string CreateLog(LogMessage LogMessage) {
            string Date = DateTime.UtcNow.ToString("hh:mm:ss tt");

            string Severity = $"[{LogMessage.Severity}]";

            return $"{Date} {Severity,9} {LogMessage.Source}: {LogMessage.Exception?.ToString() ?? LogMessage.Message}";
        }

        /// <summary>
        /// The LogToFile method creates the log file if it doesn't exist already, appends the logged message to the file,
        /// </summary>
        /// <param name="LogMessage">The LogMessage field which gives us information about the message, for example the type of
        /// exception we have run into, the severity of the exception and the message of the exception to log.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        private async Task LogToFile(LogMessage LogMessage) {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            if (!File.Exists(GetLogFile()))
                File.Create(GetLogFile()).Dispose();

            lock (LockLogFile)
                File.AppendAllText(GetLogFile(), CreateLog(LogMessage) + "\n");

            await LogToConsole(LogMessage);
        }

        /// <summary>
        /// The LogToConsole method switches the console color to the severity of the message and logs the message to the console.
        /// </summary>
        /// <param name="LogMessage">The LogMessage field which gives us information about the message, for example the type of
        /// exception we have run into, the severity of the exception and the message of the exception to log.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        private async Task LogToConsole (LogMessage LogMessage) {
            Console.ForegroundColor = LogMessage.Severity switch {
                LogSeverity.Info => ConsoleColor.Blue,
                LogSeverity.Critical => ConsoleColor.DarkRed,
                LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning => ConsoleColor.Yellow,
                LogSeverity.Verbose => ConsoleColor.Magenta,
                LogSeverity.Debug => ConsoleColor.DarkCyan,
                _ => ConsoleColor.Red,
            };

            await Console.Out.WriteLineAsync(CreateLog(LogMessage));
        }

        /// <summary>
        /// The Set Output To Locked command locks the output of both the file logger and console logger based on the parameter.
        /// </summary>
        /// <param name="IsLocked">A boolean which specifies if the output feed is locked or not.</param>
        public void SetOutputToLocked(bool IsLocked) {
            LockedCMDOut = IsLocked;
        }

        /// <summary>
        /// Gets the LogFile from the instance of the class, initialized in the constructor.
        /// </summary>
        /// <returns>Returns the filepath of where the log file is.</returns>
        public string GetLogFile() {
            return LogFile;
        }

    }

}
