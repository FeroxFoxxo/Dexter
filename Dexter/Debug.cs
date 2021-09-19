using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;

namespace Dexter
{

    /// <summary>
    /// The LoggingService is used to log messages from both the DiscordSocketClient and CommandService
    /// to the console and logging file for debugging purposes.
    /// </summary>

    public static class Debug
    {

        /// <summary>
        /// The LogFile is the string of the location of the logfile. It can be used to locate and send the logfile.
        /// </summary>

        public static readonly string LogFile = Path.Combine(Directory.GetCurrentDirectory(), "Logs", $"{DateTime.UtcNow:yyyy-MM-dd}.log");

        /// <summary>
        /// The LockLogFile is an object that locks the current thread to ensure there is thread-saftey while writing to console.
        /// </summary>

        private static readonly object LockLogFile = new();

        /// <summary>
        /// The LogMessage method sees if the command output is locked and, if so, adds it to a list of backlogged messages.
        /// If it is not blocked it simply outputs the message to the console, running through any previously blocked messages
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task LogMessageAsync(string Message, LogSeverity Severity = LogSeverity.Info)
        {
            MemberInfo Base = new StackFrame(4).GetMethod().DeclaringType;

            string MethodName = Base.Name;

            int Index = MethodName.IndexOf(">d__");

            if (Index != -1)
                MethodName = MethodName.Substring(0, Index).Replace("<", "");

            await LogMessageAsync (new LogMessage(Severity, $"{Base.DeclaringType.Name}.{MethodName}", Message));
        }

        /// <summary>
        /// The CreateLog method creates the log string from an instance of LogMessage and returns it.
        /// </summary>
        /// <param name="LogMessage">The LogMessage field which gives us information about the message, for example the type of
        /// exception we have run into, the severity of the exception and the message of the exception to log.</param>
        /// <returns>A string of the logged message.</returns>

        private static string CreateLog(LogMessage LogMessage)
        {
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

        public static async Task LogMessageAsync (LogMessage LogMessage)
        {
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

        private static async Task LogToConsole(LogMessage LogMessage)
        {
            // Firstly we switch the log message based on the severity of the error.
            Console.ForegroundColor = LogMessage.Severity switch
            {
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
