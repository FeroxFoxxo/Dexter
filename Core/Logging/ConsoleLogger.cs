using System;

namespace Dexter.Core {
    public static class ConsoleLogger {
        public static void Log(string message) {
            Console.WriteLine($"\n {DateTime.Now:G} - {message}");
            Console.WriteLine(" ");
        }

        public static void LogError(string message) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n {DateTime.Now:G} - {message}");
            Console.ResetColor();
        }
    }
}
