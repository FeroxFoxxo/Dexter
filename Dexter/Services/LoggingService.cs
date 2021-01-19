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
    
    public class LoggingService : Service {

        /// <summary>
        /// The CommandService is used to hook into the Log delegate to run LogMessageAsync.
        /// </summary>
        
        public CommandService CommandService { get; set; }

        /// <summary>
        /// The LockedCMDOut is a boolean of whether or not the output of the console is locked or not.
        /// </summary>
        
        public bool LockedCMDOut = false;

        /// <summary>
        /// The LogFile is the string of the location of the logfile. It can be used to locate and send the logfile.
        /// </summary>
        
        public string LogFile = Path.Combine(Directory.GetCurrentDirectory(), "Logs", $"{DateTime.UtcNow:yyyy-MM-dd}.log");

        /// <summary>
        /// The LockLogFile is an object that locks the current thread to ensure there is thread-saftey while writing to console.
        /// </summary>
        
        public readonly object LockLogFile = new ();

        /// <summary>
        /// The BackloggedMessages is a list that can be used to store console logs while Dexter is initializing, so they are not cleared on ready.
        /// </summary>
        
        public readonly List<LogMessage> BackloggedMessages = new ();

        /// <summary>
        /// The Initialize override hooks into both the Commands.Log event and the Client.Log event to run LogMessageAsync.
        /// </summary>
        
        public override void Initialize() {
            DiscordSocketClient.Log += LogMessageAsync;
            CommandService.Log += LogMessageAsync;
        }

        /// <summary>
        /// The TryLogMessage method sees if the command output is locked and, if so, adds it to a list of backlogged messages.
        /// If it is not blocked it simply outputs the message to the console, running through any previously blocked messages
        /// </summary>
        /// <param name="LogMessage">The LogMessage field which gives us information about the message, for example the type of
        /// exception we have run into, the severity of the exception and the message of the exception to log.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task LogMessageAsync(LogMessage LogMessage) {
            // If the CMD is locked we add the message to a backlog of messages and log it to the console, not writing to the file.
            if (LockedCMDOut) {
                await LogToConsole(LogMessage);
                BackloggedMessages.Add(LogMessage);
                return;
            }

            // If we have backlogged messages, we loop through them and clear them out.
            if (BackloggedMessages.Count > 0) {
                foreach (LogMessage Message in BackloggedMessages)
                    await LogToFile(Message);

                BackloggedMessages.Clear();
            }

            // We finally log the message to the console and file if it is not locked.
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
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        private async Task LogToFile(LogMessage LogMessage) {
            // We first get the log directory and check to see if the file and folder exist. If not, create.
            string TemporaryLogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

            if (!Directory.Exists(TemporaryLogDirectory))
                Directory.CreateDirectory(TemporaryLogDirectory);

            if (!File.Exists(LogFile))
                File.Create(LogFile).Dispose();

            // We then write to the log file with the message.
            lock (LockLogFile)
                File.AppendAllText(LogFile, CreateLog(LogMessage) + "\n");

            // Finally we log to the console.
            await LogToConsole(LogMessage);
        }

        /// <summary>
        /// The LogToConsole method switches the console color to the severity of the message and logs the message to the console.
        /// </summary>
        /// <param name="LogMessage">The LogMessage field which gives us information about the message, for example the type of
        /// exception we have run into, the severity of the exception and the message of the exception to log.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        private static async Task LogToConsole (LogMessage LogMessage) {
            // Firstly we switch the log message based on the severity of the error.
            Console.ForegroundColor = LogMessage.Severity switch {
                LogSeverity.Info => ConsoleColor.Blue,
                LogSeverity.Critical => ConsoleColor.DarkRed,
                LogSeverity.Error => ConsoleColor.Red,
                LogSeverity.Warning => ConsoleColor.Yellow,
                LogSeverity.Verbose => ConsoleColor.Magenta,
                LogSeverity.Debug => ConsoleColor.DarkCyan,
                _ => ConsoleColor.Red,
            };

            // Finally we log to the console.
            await Console.Out.WriteLineAsync(CreateLog(LogMessage));
        }

    }

}
