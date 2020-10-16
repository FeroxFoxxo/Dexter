using System.Text.RegularExpressions;

namespace Dexter.Core.Abstractions {
    public static class StringExtensions {
        public static string Prettify(this string Name)
            => Regex.Replace(Name, @"(?<!^)(?=[A-Z])", " ");

        public static string Sanitize(this string Name)
            => Name.Replace("Commands", string.Empty);
    }
}
