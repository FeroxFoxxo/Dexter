using Discord;
using System;
using System.Threading.Tasks;

namespace Dexter.Core {
    public static class ConsoleLogger {
        public static Task LogDiscord(LogMessage evt) {
            if (evt.Severity == LogSeverity.Critical) {
                LogError(evt.Message);
            } else {
                Log(evt.Message);
            }

            return Task.CompletedTask;
        }

        public static void Log(string message) {
            Console.Write($"\n\n {DateTime.Now:G} - {message} ");
        }

        public static void LogError(string message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"\n\n {DateTime.Now:G} - {message} ");
        }
    }
}
