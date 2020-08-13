using Discord;
using System;
using System.Threading.Tasks;

namespace Dexter.Core {
    public static class ConsoleLogger {
        public static Task LogDiscord(LogMessage Event) {
            if (Event.Severity == LogSeverity.Critical)
                LogError(Event.Message);
            else
                Log(Event.Message);

            return Task.CompletedTask;
        }

        public static void Log(string Message) {
            Console.Write($"\n\n {DateTime.Now:G} - {Message} ");
        }

        public static void LogError(string Message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"\n\n {DateTime.Now:G} - {Message} ");
            Console.ResetColor();
        }
    }
}
