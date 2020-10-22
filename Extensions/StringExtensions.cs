using System.Text.RegularExpressions;

namespace Dexter.Abstractions {
    public static class StringExtensions {

        private static readonly string[] SensitiveCharacters = { "\\", "*", "_", "~", "`", "|", ">", "[", "(" };

        public static string Prettify(this string Name)
            => Regex.Replace(Name, @"(?<!^)(?=[A-Z])", " ");

        public static string Sanitize(this string Name)
            => Name.Replace("Commands", string.Empty);

        public static string SanitizeMarkdown(this string Text) {
            foreach (string Unsafe in SensitiveCharacters)
                Text = Text.Replace(Unsafe, $"\\{Unsafe}");
            return Text;
        }

    }
}
