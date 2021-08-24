﻿using Dexter.Configurations;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dexter.Helpers
{

    /// <summary>
    /// Holds a variety of tools to deal with organic string management/manipulation.
    /// </summary>

    public static class LanguageHelper
    {

        private enum ArticleType
        {
            None,
            Uppercase,
            Lowercase
        }

        private enum PluralType
        {
            None,
            Plural
        }

        private enum PossessiveType
        {
            None,
            Possessive
        }

        /// <summary>
        /// Randomizes special groups of characters in <paramref name="Predicate"/> and fills them in with corresponding terms in <paramref name="TermBanks"/>.
        /// </summary>
        /// <remarks><para>The way to mark an expression for randomization is to wrap it in braces ("{}"). The format of each expression must be as follows: {IDENTIFIERX}, where IDENTIFIER is a key in <paramref name="TermBanks"/> and X is a positive integer value, every expression with the same identifier and value will be swapped for the same term.</para>
        /// <para>Special Identifiers exist, which do not run through terms: 'a' will try to guess the most probable expression of the indefinite article.</para></remarks>
        /// <param name="Predicate">The message whose terms are to be randomized.</param>
        /// <param name="TermBanks">A string-string[] dictionary where the keys are the explicit identifier of each TermClass and the values are list of terms those expressions can be substituted with.</param>
        /// <param name="RNG">A Random Number Generator used to extract random terms from <paramref name="TermBanks"/>.</param>
        /// <param name="Config">The LanguageConfiguration instance to run this process with.</param>
        /// <returns>A <c>string</c> holding the new, randomized predicate.</returns>

        public static string RandomizePredicate(string Predicate, Dictionary<string, string[]> TermBanks, Random RNG, LanguageConfiguration Config)
        {
            HashSet<TermClass> Terms = new HashSet<TermClass>();

            foreach (KeyValuePair<string, string[]> k in TermBanks)
            {
                Terms.Add(new TermClass(k.Key, k.Value));
            }

            StringBuilder NewPredicate = new StringBuilder(Predicate.Length * 2);

            ArticleType ResolveArticle = ArticleType.None;
            PluralType ResolvePlural = PluralType.None;
            PossessiveType ResolvePossessive = PossessiveType.None;

            while (Predicate.Length > 0)
            {
                int InsertIndex = Predicate.IndexOf(Config.TermInsertionStartIndicator);
                if (InsertIndex == -1)
                {
                    NewPredicate.Append(Predicate);
                    break;
                }

                int EndIndex = Predicate.IndexOf(Config.TermInsertionEndIndicator);
                if (EndIndex == -1)
                {
                    throw new FormatException($"There was an error parsing predicate {Predicate}, unbalanced braces. Please contact the developer team.");
                }

                NewPredicate.Append(Predicate[..InsertIndex]);

                string CompareString = Predicate[(InsertIndex + 1)..EndIndex];

                //SPECIAL CASES
                if (CompareString is "a" or "A")
                {
                    ResolveArticle = CompareString == "a" ? ArticleType.Lowercase : ArticleType.Uppercase;
                }
                else if (CompareString.ToLower() == "plural")
                {
                    ResolvePlural = PluralType.Plural;
                }
                else if (CompareString.ToLower() == "\'s")
                {
                    ResolvePossessive = PossessiveType.Possessive;
                }
                else
                {
                    //Default case
                    foreach (TermClass Term in Terms)
                    {
                        int Index = Term.CheckReplace(CompareString, Config);

                        if (Index == (int)Config.ErrorCodeInvalidArgument)
                            throw new IndexOutOfRangeException($"There was an error parsing the number in term call \"{CompareString}\" within \"{Predicate}\". Please contact the developer team.");

                        if (Index >= 0)
                        {
                            string newTerm = Term.GetOrGenerateCached(Index, RNG, Config);

                            if (ResolvePlural != PluralType.None)
                            {
                                newTerm = newTerm.GuessPlural(Config);
                            }

                            if (ResolvePossessive != PossessiveType.None)
                            {
                                newTerm = newTerm.Possessive();
                            }

                            if (ResolveArticle != ArticleType.None)
                            {
                                newTerm = $"{GuessIndefiniteArticle(newTerm, ResolveArticle == ArticleType.Uppercase)} {newTerm}";
                            }

                            NewPredicate.Append(newTerm);

                            ResolvePlural = PluralType.None;
                            ResolveArticle = ArticleType.None;
                            ResolvePossessive = PossessiveType.None;
                            break;
                        }
                    }
                }

                Predicate = Predicate[(EndIndex + 1)..];
            }

            return NewPredicate.ToString();
        }

        /// <summary>
        /// Checks whether an "IDENTIFIERX" expression shares an identifier with <paramref name="Term"/>, and returns the index X.
        /// </summary>
        /// <param name="Str">The raw form of the identifier-index expression, without braces.</param>
        /// <param name="Term">The TermClass to compare <paramref name="Str"/> against.</param>
        /// <param name="Config">The LanguageConfiguration instance to run this process with.</param>
        /// <returns>The number accompanying the raw expression <paramref name="Str"/> if their identifiers match, Invalid Number Error Code if the term X can't be parsed to a positive integer, -1 otherwise.</returns>

        private static int CheckReplace(this TermClass Term, string Str, LanguageConfiguration Config)
        {
            if (!Str.StartsWith(Term.Identifier))
                return -1;

            if (int.TryParse(Str[Term.Identifier.Length..], out int Index))
            {
                return Index >= 0 ? Index : (int)Config.ErrorCodeInvalidArgument;
            }

            return (int)Config.ErrorCodeInvalidArgument;
        }

        /// <summary>
        /// Holds a list of terms related by a common Identifier and provides a way to generate random terms.
        /// </summary>

        internal class TermClass
        {

            public readonly string Identifier;
            private readonly string[] Bank;
            private readonly List<string> Cache;

            public TermClass(string Identifier, string[] Bank)
            {
                this.Identifier = Identifier;
                this.Bank = Bank;
                Cache = new List<string>();
            }

            /// <summary>
            /// Gets a cached term located at <paramref name="Index"/>, if Cache doesn't have those many elements, it generates elements up to <paramref name="Index"/>.
            /// </summary>
            /// <remarks>The TermClass will attempt to generate new terms that aren't in its cache. To disable this, set <paramref name="MaxRetries"/> to 0.</remarks>
            /// <param name="Index">The index to get from the cache of terms stored in this TermClass.</param>
            /// <param name="Generator">A random number generator.</param>
            /// <param name="Config">The LanguageConfiguration instance to run this process with.</param>
            /// <param name="MaxRetries">The maximum amount of attempts to generate a novel term from the bank.</param>
            /// <returns>The term at position <paramref name="Index"/> within the Cache.</returns>

            public string GetOrGenerateCached(int Index, Random Generator, LanguageConfiguration Config, int MaxRetries = -1)
            {
                if (MaxRetries < 0)
                    MaxRetries = (int)Config.TermRepetitionAversionFactor;

                while (Cache.Count <= Index)
                {
                    Cache.Add(Bank[Generator.Next(Bank.Length)]);

                    //If this term is already in the cache, try to replace it for a new, random one.
                    for (int i = 0; i < MaxRetries && Cache.IndexOf(Cache[^1]) < Cache.Count - 1; i++)
                    {
                        Cache[^1] = Bank[Generator.Next(Bank.Length)];
                    }
                }

                return Cache[Index];
            }
        }

        private static readonly Dictionary<long, string> BasicUnits = new()
        {
            { 1000000000000, "T" },
            { 1000000000, "B" },
            { 1000000, "M" },
            { 1000, "K" }
        };

        /// <summary>
        /// Represents a units dictionary for binary memory units.
        /// </summary>

        public static readonly Dictionary<long, string> ByteUnits = new()
        {
            { 1099511627776, "TB" },
            { 1073741824, "GB" },
            { 1048576, "MB" },
            { 1024, "KB" }
        };

        /// <summary>
        /// Represents a units dictionary for metric prefixes up to "tera"
        /// </summary>

        public static readonly Dictionary<long, string> MetricPrefixes = new()
        {
            { 1000000000000, "T" },
            { 1000000000, "G" },
            { 1000000, "M" },
            { 1000, "K" }
        };

        /// <summary>
        /// Converts an XP amount into a shortened version using suffixes.
        /// </summary>
        /// <param name="v">The value to simplify.</param>
        /// <param name="units">The units dictionary; if <see langword="null"/>, it will default to <see cref="BasicUnits"/>.</param>
        /// <returns>A string containing the shortened value.</returns>

        public static string ToUnit(this long v, Dictionary<long, string> units = null)
        {
            if (units is null) units = BasicUnits;
            foreach (KeyValuePair<long, string> kvp in units)
            {
                if (v >= kvp.Key)
                {
                    return $"{(float)v / kvp.Key:G3}{kvp.Value}";
                }
            }
            return v.ToString();
        }

        /// <summary>
        /// Will attempt to guess whether the indefinite article should be 'a' or 'an' based on <paramref name="NextWord"/>.
        /// </summary>
        /// <param name="NextWord">A string describing what follows the article.</param>
        /// <param name="Capitalize">Whether to capitalize the first letter of the article.</param>
        /// <returns>A string, either "a", "an", or "a(n)", where the character 'a' is capitalized if <paramref name="Capitalize"/> is set to <see langword="true"/>.</returns>

        public static string GuessIndefiniteArticle(string NextWord, bool Capitalize = false)
        {
            string Relevant = NextWord.Trim().Split(' ')[0].ToLower();

            return (Relevant[0]) switch
            {
                'a' or 'e' or 'i' or 'o' => Capitalize ? "An" : "an",
                'h' or 'u' => Capitalize ? "A(n)" : "a(n)",
                _ => Capitalize ? "A" : "a",
            };
        }

        /// <summary>
        /// Guesses the most likely plural form of a noun from a set of English pluralization rules and irregular plurals.
        /// </summary>
        /// <param name="Singular">The singular noun to pluralize.</param>
        /// <param name="Config">The LanguageConfiguration instance to run this process with.</param>
        /// <returns>A string containing the pluralized form of <paramref name="Singular"/>.</returns>

        public static string GuessPlural(this string Singular, LanguageConfiguration Config)
        {
            string LowerSingular = Singular.ToLower();

            if (Config.IrregularPlurals.ContainsKey(LowerSingular))
            {
                return Config.IrregularPlurals[LowerSingular].MatchCase(Singular);
            }
            else if (LowerSingular.EndsWith("on"))
            {
                return Singular[..^2] + "a";
            }
            else if (LowerSingular.EndsWith("um"))
            {
                return Singular[..^2] + "a";
            }
            else if (LowerSingular.EndsWith("us"))
            {
                return Singular[..^2] + "i";
            }
            else if (LowerSingular.EndsWith("is"))
            {
                return Singular[..^2] + "es";
            }
            else if (LowerSingular.EndsWith("ex") || LowerSingular.EndsWith("ix"))
            {
                return Singular[..^2] + "ices";
            }
            else if (LowerSingular[^1] is 's' or 'z')
            {
                if (LowerSingular.Length > 2 && LowerSingular[^3].IsConsonant() && LowerSingular[^2].IsVowel() && LowerSingular[^1].IsConsonant())
                {
                    return Singular + Singular[^1] + "es";
                }
                else
                {
                    return Singular + "es";
                }
            }
            else if (LowerSingular.EndsWith("fe"))
            {
                return Singular[..^2] + "ves";
            }
            else if (LowerSingular.EndsWith("f"))
            {
                return Singular[..^1] + "ves";
            }
            else if (LowerSingular.EndsWith("y"))
            {
                return Singular[^2].IsVowel() ? Singular + "s" : Singular[^1] + "ies";
            }
            else if (LowerSingular[^1] is 'o' or 'x' || LowerSingular.EndsWith("sh") || LowerSingular.EndsWith("ch"))
            {
                return Singular + "es";
            }

            return Singular + "s";
        }

        /// <summary>
        /// Limits the given string <paramref name="s"/> to a length <paramref name="maxLength"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="maxLength"></param>
        /// <returns>A substring of <paramref name="s"/> ending in "..." if its length is greater than <paramref name="maxLength"/>, otherwise <paramref name="s"/></returns>

        public static string TruncateTo(this string s, int maxLength)
        {
            if (s.Length > maxLength)
            {
                if (maxLength < 3) return "...";
                return s[..(maxLength - 3)] + "...";
            }

            return s;
        }

        /// <summary>
        /// A set of characters that discord uses for formatting.
        /// </summary>

        public static readonly HashSet<char> DiscordRichTextChars = new HashSet<char>() { '*', '_', '`', '|' };

        /// <summary>
        /// Truncates a string to a given length similar to the <see cref="TruncateTo"/> method, but ignores characters marked in <paramref name="ignoreChars"/>.
        /// </summary>
        /// <param name="s">The original string to truncate.</param>
        /// <param name="maxLength">The maximum length to truncate <paramref name="s"/> to.</param>
        /// <param name="ignoreChars">The set of characters to ignore for counting.</param>
        /// <returns></returns>

        public static string TruncateToSpecial(this string s, int maxLength, HashSet<char> ignoreChars)
        {
            StringBuilder b = new(maxLength);
            int counter = 0;
            foreach (char c in s)
            {
                if (ignoreChars.Contains(c)) b.Append(c);
                else
                {
                    if (++counter > maxLength - 3)
                    {
                        b.Append("...");
                        break;
                    }
                    b.Append(c);
                }
            }
            return b.ToString();
        }

        /// <summary>
        /// Computes the length of a string ignoring all characters in <paramref name="ignoreChars"/>.
        /// </summary>
        /// <param name="s">The string whose length is to be computed.</param>
        /// <param name="ignoreChars">The set of characters which will not contribute towards length.</param>
        /// <returns>The length of the given string <paramref name="s"/> if all characters in <paramref name="ignoreChars"/> were removed from it.</returns>

        public static int LengthSpecial(this IReadOnlyCollection<char> s, HashSet<char> ignoreChars)
        {
            int result = 0;
            foreach (char c in s)
            {
                if (!ignoreChars.Contains(c)) result++;
            }
            return result;
        }

        /// <summary>
        /// Computes the length of a <see cref="StringBuilder"/> ignoring all characters in <paramref name="ignoreChars"/>.
        /// </summary>
        /// <param name="b">The <see cref="StringBuilder"/> whose length is to be computed.</param>
        /// <param name="ignoreChars">The set of characters which will not contribute towards length.</param>
        /// <returns>The length of the given string represented by <paramref name="b"/> if all characters in <paramref name="ignoreChars"/> were removed from it.</returns>

        public static int LengthSpecial(this StringBuilder b, HashSet<char> ignoreChars)
        {
            int result = 0;
            for (int i = 0; i < b.Length; i++)
            {
                if (!ignoreChars.Contains(b[i])) result++;
            }
            return result;
        }

        /// <summary>
        /// Checks whether a character is a vowel in the Latin alphabet
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is a vowel, <see langword="false"/> otherwise.</returns>

        public static bool IsVowel(this char c) { return char.ToLower(c) is 'a' or 'e' or 'i' or 'o' or 'u'; }

        /// <summary>
        /// Checks whether a character is a consonant in the Latin alphabet
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is a consonant, <see langword="false"/> otherwise.</returns>

        public static bool IsConsonant(this char c) { return char.ToLower(c) >= 'a' && char.ToLower(c) <= 'z' && !IsVowel(c); }

        /// <summary>
        /// Matches the case of <paramref name="Input"/> to that of <paramref name="Case"/>.
        /// </summary>
        /// <param name="Input">The sequence of letters to convert to <paramref name="Case"/>.</param>
        /// <param name="Case">The uppercase-lowercase sequence to follow. If the length differs from <paramref name="Input"/>, case-matching will stop..</param>
        /// <returns>A string with the same letters as <paramref name="Input"/> and the same case as <paramref name="Case"/>.</returns>

        public static string MatchCase(this string Input, string Case)
        {
            int Match = Input.Length < Case.Length ? Input.Length : Case.Length;

            StringBuilder SB = new(Input.Length);

            for (int i = 0; i < Match; i++)
            {
                SB.Append(Input[i].MatchCase(Case[i]));
            }

            if (Match < Input.Length)
            {
                SB.Append(Input[Match..]);
            }

            return SB.ToString();
        }

        /// <summary>
        /// Matches the case of <paramref name="Input"/> to that of <paramref name="Case"/>.
        /// </summary>
        /// <param name="Input">The character to convert <paramref name="Case"/>.</param>
        /// <param name="Case">The uppercase or lowercase character to reference for conversion of <paramref name="Input"/>.</param>
        /// <returns>The character obtained from <paramref name="Input"/> in the same case as <paramref name="Case"/>.</returns>

        public static char MatchCase(this char Input, char Case)
        {
            if (char.IsUpper(Case))
                return char.ToUpper(Input);

            return char.ToLower(Input);
        }

        /// <summary>
        /// Obtains the ordinal form of an integer <paramref name="N"/>.
        /// </summary>
        /// <param name="N">The base number to obtain the ordinal from.</param>
        /// <returns>The string "<paramref name="N"/>st", "<paramref name="N"/>nd", "<paramref name="N"/>rd" if any are appropriate, otherwise "<paramref name="N"/>th".</returns>

        public static string Ordinal(this int N)
        {
            if (N < 0)
                N = -N;

            if (N % 100 / 10 == 1)
                return N + "th";

            return (N % 10) switch
            {
                1 => N + "st",
                2 => N + "nd",
                3 => N + "rd",
                _ => N + "th"
            };
        }

        /// <summary>
        /// Adds the appropriate form of the possessive "'s" to a term <paramref name="Input"/>.
        /// </summary>
        /// <param name="Input">The noun to add the possessive termination to.</param>
        /// <returns>"<paramref name="Input"/>'" if <paramref name="Input"/> ends in 's'. "<paramref name="Input"/>'s" otherwise.</returns>

        public static string Possessive(this string Input)
        {
            return $"{Input}'{(Input.EndsWith('s') ? "" : "s")}";
        }

        /// <summary>
        /// Creates a human-readable expression of a hexagesimal-base system (such as hours and minutes).
        /// </summary>
        /// <param name="Value">The decimal value corresponding to the <paramref name="LargeUnit"/>.</param>
        /// <param name="LargeUnit">The names of the larger unit, [0] is singular and [1] is plural.</param>
        /// <param name="SmallUnit">The names of the smaller unit, [0] is singular and [1] is plural.</param>
        /// <param name="Remainder">The decimal value that was disregarded after comparing the units.</param>
        /// <returns>A humanized expression of <paramref name="Value"/> <paramref name="LargeUnit"/>.</returns>

        public static string HumanizeSexagesimalUnits(float Value, string[] LargeUnit, string[] SmallUnit, out float Remainder)
        {
            return HumanizeOffbaseUnits(60, Value, LargeUnit, SmallUnit, out Remainder);
        }

        /// <summary>
        /// Creates a human-readable expression of a value in an arbitrary base with multiple Units. (Like feet and inches or degrees and arcminutes)
        /// </summary>
        /// <param name="Base">The base of the counting system used to differentiate <paramref name="LargeUnit"/> and <paramref name="SmallUnit"/>.</param>
        /// <param name="Value">The decimal value corresponding to the <paramref name="LargeUnit"/>.</param>
        /// <param name="LargeUnit">The names of the larger unit, [0] is singular and [1] is plural.</param>
        /// <param name="SmallUnit">The names of the smaller unit, [0] is singular and [1] is plural.</param>
        /// <param name="Remainder">The decimal value that was disregarded after comparing the units.</param>
        /// <returns>A humanized expression of <paramref name="Value"/> <paramref name="LargeUnit"/>.</returns>

        public static string HumanizeOffbaseUnits(int Base, float Value, string[] LargeUnit, string[] SmallUnit, out float Remainder)
        {
            List<string> Result = new List<string>();

            int LargeValue = (int)Value;
            int SmallValue = (int)Math.Round(Value % 1 * Base);

            Remainder = Value - LargeValue - (float)SmallValue / Base;

            if (LargeValue != 0) Result.Add($"{LargeValue} {LargeUnit[LargeValue == 1 ? 0 : 1]}");
            if (SmallValue != 0) Result.Add($"{SmallValue} {SmallUnit[SmallValue == 1 ? 0 : 1]}");

            if (Result.Count == 0) return $"0 {LargeUnit[1]}";

            return string.Join(" and ", Result);
        }

        /// <summary>
        /// Enumerates a list of objects using the typical linguistic conventions for enumeration.
        /// </summary>
        /// <param name="inputList">The list of objects to enumerate.</param>
        /// <returns>A string with the enumeration expressed in a human-readable form.</returns>

        public static string Enumerate(IEnumerable<object> inputList)
        {
            switch(inputList.Count())
            {
                case 0:
                    return "";
                case 1:
                    return inputList.First().ToString();
                case 2:
                    return $"{inputList.First()} and {inputList.Last()}";
                default:
                    string[] toEnumerate = new string[inputList.Count()];
                    int i = 0;
                    foreach(object o in inputList)
                    {
                        toEnumerate[i++] = o.ToString();
                    }
                    return $"{string.Join(", ", toEnumerate[..^1])} and {toEnumerate[^1]}";
            }
        }

        /// <summary>
        /// Extracts substrings that fit a given url schema from an <paramref name="Input"/> string.
        /// </summary>
        /// <remarks>All potential links in the string must be encapsulated in parentheses or spaces.</remarks>
        /// <param name="Input">The string to analyze and extracts urls from.</param>
        /// <returns>A <c>string[]</c> array containing a collection of substrings that matched the url pattern in <paramref name="Input"/>.</returns>

        public static string[] GetHyperLinks(this string Input)
        {
            List<string> Matches = new List<string>();

            Input = ' ' + Input + ' ';

            List<int> Openers = new List<int>();
            List<int> Closers = new List<int>();
            for (int i = 0; i < Input.Length; i++)
            {
                switch (Input[i])
                {
                    case ' ':
                        Closers.Add(i);
                        Matches.AddRange(Input.CheckForLinks(Openers, Closers));

                        Openers.Clear();
                        Closers.Clear();
                        Openers.Add(i);
                        break;
                    case ')':
                        Closers.Add(i);
                        break;
                    case '(':
                        Openers.Add(i);
                        break;
                }
            }

            return Matches.ToArray();
        }

        private static string[] CheckForLinks(this string Input, IEnumerable<int> Openers, IEnumerable<int> Closers)
        {
            List<string> Result = new List<string>();

            foreach (int o in Openers)
            {
                foreach (int c in Closers)
                {
                    if (c > o)
                    {
                        string s = Input[(o + 1)..c];
                        if (s.IsHyperLink())
                        {
                            Result.Add(s);
                        }
                    }
                }
            }

            return Result.ToArray();
        }

        /// <summary>
        /// Checks whether a given <paramref name="Input"/> string is a url.
        /// </summary>
        /// <param name="Input">The string to check.</param>
        /// <returns><see langword="true"/> if the given string is a url; otherwise <see langword="false"/>.</returns>

        public static bool IsHyperLink(this string Input)
        {
            return Regex.IsMatch(Input, @"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_=]*)?$");
        }

        /// <summary>
        /// Indicates the ways to express a date that will be parsed successfully.
        /// </summary>

        public const string DEFAULT_DATE_FORMAT_INFO = "`Month dd((,) year)`  __*OR*__  `dd Month((,) year)`  __*OR*__  `(MM/dd(/year)`";

        /// <summary>
        /// Indicates the ways to express a time that will be parsed successfully.
        /// </summary>

        public const string DEFAULT_TIME_FORMAT_INFO = "`hh:mm(:ss(.ffff)) (<AM/PM>)`  __*OR*__  `hh <AM/PM>`";

        /// <summary>
        /// Indicates the ways to express a TimeSpan Offset for a time zone that will be parsed successfully.
        /// </summary>

        public const string DEFAULT_OFFSET_FORMAT_INFO = "`TZA`  __*OR*__  `(<UTC/GMT/Z>)<+/->h(:mm)`";


        /// <summary>
        /// Attempts to parse a string expressing a date, time, and offset. All elements except for the hour and minute are optional.
        /// </summary>
        /// <param name="Input">The stringified expression of the date to be parsed into <paramref name="Time"/>.</param>
        /// <param name="CultureInfo">The Cultural Context with which to parse the date given in <paramref name="Input"/>.</param>
        /// <param name="LanguageConfiguration">The Configuration related to parsing linguistic humanized information like time zone abbreviations.</param>
        /// <param name="Time">The parsed <c>DateTimeOffset</c> extracted from <paramref name="Input"/>.</param>
        /// <param name="Error">The reason the parsing failed if it did.</param>
        /// <returns><see langword="true"/> if the parsing was successful; otherwise <see langword="false"/>.</returns>

        public static bool TryParseTime(this string Input, CultureInfo CultureInfo, LanguageConfiguration LanguageConfiguration, out DateTimeOffset Time, out string Error)
        {

            Error = "";
            Input = Input.Trim();
            Time = DateTimeOffset.Now;

            string LowerInput = Input.ToLower();

            switch (LowerInput)
            {
                case "now":
                    Time = DateTimeOffset.Now;
                    return true;
            }

            string TimeZoneMatcher = @"(((UTC|GMT|Z)?[+-][0-9]{1,2}(:[0-9]{2})?)|([A-Z][A-Za-z0-9]*))$";
            string TimeZoneSegment = Regex.Match(Input, TimeZoneMatcher).Value;

            TimeZoneData TimeZone = null;
            TimeSpan TimeZoneOffset = DateTimeOffset.Now.Offset;

            if (!string.IsNullOrEmpty(TimeZoneSegment))
            {
                if (TimeZoneSegment.Contains("+") || TimeZoneSegment.Contains("-") || LanguageConfiguration.TimeZones.ContainsKey(TimeZoneSegment))
                {
                    if (TimeZoneData.TryParse(TimeZoneSegment, LanguageConfiguration, out TimeZone))
                    {
                        TimeZoneOffset = TimeZone.TimeOffset;
                    }
                }
            }

            if (Regex.IsMatch(LowerInput, @$"(^in)|(from now\s*{TimeZoneMatcher}?[\s.]*$)", RegexOptions.IgnoreCase))
            {
                if (!TryParseSpan(Input, out TimeSpan Span, out string NewError))
                {
                    Error = NewError;
                    return false;
                }
                Time = Time.Add(Span).Add(TimeSpan.FromMilliseconds(100)).ToOffset(TimeZoneOffset);
                return true;
            }
            else if (Regex.IsMatch(LowerInput, @$"ago\s*{TimeZoneMatcher}?[\s.]*$", RegexOptions.IgnoreCase))
            {
                if (!TryParseSpan(Input, out TimeSpan Span, out string NewError))
                {
                    Error = NewError;
                    return false;
                }
                Time = Time.Subtract(Span).Subtract(TimeSpan.FromMilliseconds(100)).ToOffset(TimeZoneOffset);
                return true;
            }

            foreach (Match m in Regex.Matches(Input, @"[0-9](st|nd|rd|th)"))
            {
                Input = $"{Input[..m.Index]}{m.Value[0]}{(Input.Length > m.Index + m.Length ? Input[(m.Index + m.Length)..] : "")}";
            }

            string DateStrSegment = Regex.Match(Input, @"(^|\s)(([A-Za-z]{3,}\s[0-9]{1,2})|([0-9]{1,2}\s[A-Za-z]{3,}))((,|\s)\s?[0-9]{2,5}(\s|$))?").Value.Trim();
            string DateNumSegment = Regex.Match(Input, @"[0-9]{1,2}\/[0-9]{1,2}(\/[0-9]{2,5})?").Value;
            string TimeSimplifiedSegment = Regex.Match(Input, @"(^|\s)[0-9]{1,2}\s?[pa]m(\s|$)", RegexOptions.IgnoreCase).Value;
            string TimeSegment = Regex.Match(Input, @"[0-9]{1,2}:[0-9]{1,2}(:[0-9]{1,2}(.[0-9]+)?)?(\s?(a|p)m)?", RegexOptions.IgnoreCase).Value;

            DateTimeOffset OffsetNow = DateTimeOffset.Now.ToOffset(TimeZoneOffset);

            int Year = OffsetNow.Year;
            int Month = OffsetNow.Month;
            int Day = OffsetNow.Day;
            int Hour = OffsetNow.Hour;
            int Minute = OffsetNow.Minute;
            float Second = 0;

            if (!string.IsNullOrEmpty(DateStrSegment))
            {
                DateStrSegment = DateStrSegment.Replace(", ", " ").Replace(",", " ");

                string[] MDY = DateStrSegment.Split(" ");
                string dd;

                Month = ParseMonth(MDY[0]);
                if (Month < 0)
                {
                    Month = ParseMonth(MDY[1]);
                    if (Month < 0)
                    {
                        Error = $"Failed to parse \"{MDY[0]}\" OR \"{MDY[1]}\" into a valid Month.";
                        return false;
                    }
                    dd = MDY[0];
                }
                else
                {
                    dd = MDY[1];
                }

                if (!int.TryParse(dd, out Day))
                {
                    Error = $"Failed to parse {dd} into a valid Day of the Month.";
                    return false;
                }

                if (Day < 0 || Day > 31) { return false; }

                if (MDY.Length > 2)
                {
                    if (!int.TryParse(MDY[2], out Year))
                    {
                        Error = $"Failed to parse {MDY[2]} into a valid year!";
                        return false;
                    }
                    if (Year < 100) Year += 2000; //YY parsing
                    if (Year > 10000) Year -= 10000; //Human Era Parsing
                    if (Year < 100 || Year > 3000)
                    {
                        Error = $"Year {Year} is outside the range of valid accepted years (must be between 100 and 3000)";
                        return false;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(DateNumSegment))
            {
                if (DateNumSegment.Split("/").Length < 3)
                {
                    DateNumSegment += $"/{Year}";
                }

                DateTime Subparse;
                try
                {
                    Subparse = DateTime.Parse(DateNumSegment, CultureInfo);
                }
                catch (FormatException e)
                {
                    Error = e.Message;
                    return false;
                }

                Day = Subparse.Day;
                Month = Subparse.Month;
                Year = Subparse.Year;
            }

            TimeMeridianDiscriminator TMD = TimeMeridianDiscriminator.H24;

            if (!string.IsNullOrEmpty(TimeSimplifiedSegment))
            {
                TMD = TimeSimplifiedSegment.Trim()[^2] is 'p' or 'P' ? TimeMeridianDiscriminator.PM : TimeMeridianDiscriminator.AM;
                Hour = int.Parse(TimeSimplifiedSegment.Trim()[..^2]);
                Minute = 0;
            }
            else
            {
                if (string.IsNullOrEmpty(TimeSegment))
                {
                    if (string.IsNullOrEmpty(DateNumSegment) && string.IsNullOrEmpty(DateStrSegment))
                    {
                        Error = "A time or day must be provided! Time segments are formatted as: `hh:mm(:ss) (<am/pm>)`";
                        return false;
                    }
                    else
                    {
                        TMD = TimeMeridianDiscriminator.H24;
                        Hour = 0;
                        Minute = 0;
                        Second = 0;
                    }
                }
                else
                {
                    if (TimeSegment[^1] is 'm' or 'M')
                    {
                        TMD = TimeSegment[^2] is 'p' or 'P' ? TimeMeridianDiscriminator.PM : TimeMeridianDiscriminator.AM;
                        TimeSegment = TimeSegment[..^2];
                    }

                    string[] hmsf = TimeSegment.Trim().Split(":");
                    Hour = int.Parse(hmsf[0]);
                    Minute = int.Parse(hmsf[1]);

                    if (hmsf.Length > 2) Second = float.Parse(hmsf[2]);
                }
            }

            if (TMD == TimeMeridianDiscriminator.AM && Hour == 12) Hour = 0;
            else if (TMD == TimeMeridianDiscriminator.PM && Hour != 12) Hour += 12;

            try
            {
                Time = new DateTimeOffset(new DateTime(Year, Month, Day, Hour, Minute, (int)Second, (int)(Second % 1 * 1000)), TimeZoneOffset);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Error = $"Impossible to parse to a valid time! Are you sure the month you chose has enough days?\n" +
                    $"Selected numbers are Year: {Year}, Month: {Month}, Day: {Day}, Hour: {Hour}, Minute: {Minute}, Second: {Second}, Time Zone: {TimeZone?.ToString() ?? TimeZoneData.ToTimeZoneExpression(TimeZoneOffset)}.\n[{e.Message}]";
                return false;
            }

            return true;
        }

        private enum TimeMeridianDiscriminator
        {
            H24,
            AM,
            PM
        }

        /// <summary>
        /// Attempts to parse a Month given a CultureInfo for Month Names and Abbreviated Month Names.
        /// </summary>
        /// <param name="input">An abbreviated or complete month name in accordance to <paramref name="cultureInfo"/>, case-insensitive.</param>
        /// <param name="cultureInfo">The contextual CultureInfo containing the calendar information and month names.</param>
        /// <returns><c>-1</c> if the parsing is unsuccessful, otherwise the number corresponding to the month (1 for January, 2 for February... etc.)</returns>

        public static int ParseMonth(this string input, CultureInfo cultureInfo = null)
        {
            if (cultureInfo == null) cultureInfo = CultureInfo.InvariantCulture;

            input = input.ToLower();

            for (int i = 0; i < cultureInfo.DateTimeFormat.MonthNames.Length; i++)
            {
                if (input == cultureInfo.DateTimeFormat.MonthNames[i].ToLower() || input == cultureInfo.DateTimeFormat.AbbreviatedMonthNames[i].ToLower())
                {
                    return i + 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Attempts to parse a Month given a CultureInfo for Month Names and Abbreviated Month Names.
        /// </summary>
        /// <param name="input">An abbreviated or complete month name in accordance to <paramref name="cultureInfo"/>, case-insensitive.</param>
        /// <param name="cultureInfo">The contextual CultureInfo containing the calendar information and month names.</param>
        /// <returns>The Month enum corresponding to the parsed month, or <see cref="Month.None"/> if none match.</returns>

        public static Month ParseMonthEnum(this string input, CultureInfo cultureInfo = null)
        {
            int m = ParseMonth(input, cultureInfo);

            if (m < 0) return Month.None;
            else return (Month)m;
        }

        /// <summary>
        /// Attempts to obtain a <see cref="Weekday"/> from a string representation of it.
        /// </summary>
        /// <param name="input">The raw stringified expression.</param>
        /// <param name="weekday">The result of the operation if successful.</param>
        /// <param name="feedback">The description of the result or error in the operation.</param>
        /// <returns><see langword="true"/> if the parsing is successful, otherwise <see langword="false"/>.</returns>

        public static bool TryParseWeekday(this string input, out Weekday weekday, out string feedback)
        {

            HashSet<string> days = CultureInfo.InvariantCulture.DateTimeFormat.DayNames.ToHashSet();
            input = input.ToLower();
            weekday = Weekday.None;

            for (int i = 0; i < input.Length; i++)
            {
                days.RemoveWhere((d) => d.Length < i || char.ToLower(d[i]) != input[i]);
            }

            if (days.Count == 0)
            {
                feedback = $"No days of the week start with the sequence {input}.";
                return false;
            }
            else if (days.Count > 1)
            {
                feedback = $"Input is ambiguouos between the following possible terms: {string.Join(", ", days)}.";
                return false;
            }

            if (!Enum.TryParse(days.First(), true, out weekday))
            {
                feedback = $"Unable to parse {days.First()} to a valid standard weekday, if you're using the English language, this is an error; please contact a developer so it can be fixed.";
                return false;
            }
            feedback = $"Parsed {weekday} from {input}.";
            return true;
        }

        /// <summary>
        /// Represents a day of the week.
        /// </summary>

        public enum Weekday : byte
        {
            /// <summary>
            /// Represents the 1st day of the work week.
            /// </summary>        
            Monday,
            /// <summary>
            /// Represents the 2nd day of the work week.
            /// </summary>  
            Tuesday,
            /// <summary>
            /// Represents the 3rd day of the work week.
            /// </summary>  
            Wednesday,
            /// <summary>
            /// Represents the 4th day of the work week.
            /// </summary>  
            Thursday,
            /// <summary>
            /// Represents the 5th day of the work week.
            /// </summary>  
            Friday,
            /// <summary>
            /// Represents the 1st day of the weekend.
            /// </summary> 
            Saturday,
            /// <summary>
            /// Represents the 2nd day of the weekend.
            /// </summary>  
            Sunday,
            /// <summary>
            /// Represents a non-valid weekday.
            /// </summary>
            None
        }

        /// <summary>
        /// Represents a month in the year
        /// </summary>

        public enum Month : byte
        {
            /// <summary>
            /// Represents an invalid month of the year.
            /// </summary>
            None,
            /// <summary>
            /// Represents the 1st month of the year.
            /// </summary>
            January,
            /// <summary>
            /// Represents the 2nd month of the year.
            /// </summary>
            February,
            /// <summary>
            /// Represents the 3rd month of the year.
            /// </summary>
            March,
            /// <summary>
            /// Represents the 4th month of the year.
            /// </summary>
            April,
            /// <summary>
            /// Represents the 5th month of the year.
            /// </summary>
            May,
            /// <summary>
            /// Represents the 6th month of the year.
            /// </summary>
            June,
            /// <summary>
            /// Represents the 7th month of the year.
            /// </summary>
            July,
            /// <summary>
            /// Represents the 8th month of the year.
            /// </summary>
            August,
            /// <summary>
            /// Represents the 9th month of the year.
            /// </summary>
            September,
            /// <summary>
            /// Represents the 10th month of the year.
            /// </summary>
            October,
            /// <summary>
            /// Represents the 11th month of the year.
            /// </summary>
            November,
            /// <summary>
            /// Represents the 12th month of the year.
            /// </summary>
            December
        }

        /// <summary>
        /// Attempts to find a time zone by the abbreviated name of <paramref name="input"/> and returns it as a TimeSpan to be used as an Offset. 
        /// </summary>
        /// <param name="input">The abbreviation of the time zone as it appears in <paramref name="languageConfiguration"/>.</param>
        /// <param name="languageConfiguration">The Config file containing data on Time Zone names and their respective offsets.</param>
        /// <param name="timeZone">The output value of the parsed <paramref name="input"/>, or <see langword="null"/> if it can't be parsed.</param>
        /// <returns><see langword="true"/> if the parsing was successful; otherwise <see langword="false"/>.</returns>

        public static bool TryParseTimeZone(this string input, LanguageConfiguration languageConfiguration, out TimeZoneData timeZone)
        {
            if (languageConfiguration.TimeZones.ContainsKey(input))
            {
                timeZone = languageConfiguration.TimeZones[input];
                return true;
            }
            else if (languageConfiguration.TimeZones.ContainsKey(input.ToUpper()))
            {
                timeZone = languageConfiguration.TimeZones[input.ToUpper()];
                return true;
            }

            timeZone = null;
            return false;
        }

        /// <summary>
        /// Attempts to parse a timespan out of humanized terms
        /// </summary>
        /// <param name="Input">A stringified time expression consisting of a series of numbers followed by units.</param>
        /// <param name="Span">The output span resulting from the parsing of <paramref name="Input"/>.</param>
        /// <param name="Error">Empty string if there are no errors, otherwise describes why it failed.</param>
        /// <returns><see langword="true"/> if the parsing was successful, otherwise <see langword="false"/>.</returns>

        public static bool TryParseSpan(this string Input, out TimeSpan Span, out string Error)
        {
            Error = "";
            Span = TimeSpan.Zero;

            Dictionary<TimeUnit, string> RegExps = new Dictionary<TimeUnit, string> {
                { TimeUnit.Millisecond, @"(ms)|((milli)(second)?s?)" },
                { TimeUnit.Second, @"s(ec(ond)?s?)?" },
                { TimeUnit.Minute, @"m(in(ute)?s?)?" },
                { TimeUnit.Hour, @"h((ou)?rs?)?" },
                { TimeUnit.Day, @"d(ays?)?" },
                { TimeUnit.Week, @"w((ee)?ks?)?" },
                { TimeUnit.Month, @"mon(ths?)?" },
                { TimeUnit.Year, @"y((ea)?rs?)?" },
                { TimeUnit.Century, @"centur(y|ies)" },
                { TimeUnit.Millenium, @"millenn?i(um|a)" }
            };

            foreach (KeyValuePair<TimeUnit, string> Unit in RegExps)
            {
                string Parsable = Regex.Match(Input, $@"[0-9.]+\s*({Unit.Value})(([0-9]+)|\s|$)", RegexOptions.IgnoreCase).Value;
                if (string.IsNullOrEmpty(Parsable)) continue;
                for (int i = 0; i < Parsable.Length; i++)
                {
                    if (!char.IsDigit(Parsable[i]))
                    {
                        if (!double.TryParse(Parsable[..i], out double Factor))
                        {
                            Error = $"Failed to parse number \"${Parsable[..i]}\" for unit ${Unit.Key}.";
                            return false;
                        }
                        try
                        {
                            Span = Span.Add(Factor * UnitToTime[Unit.Key]);
                        }
                        catch (OverflowException e)
                        {
                            Error = $"Unable to create time! The duration you specified is too long.\n[{e.Message}]";
                            return false;
                        }
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Represents a range of time units ranging from MILLISECOND to MILLENIUM.
        /// </summary>
        public enum TimeUnit
        {
            /// <summary>
            /// Represents a thousandth of a second
            /// </summary>
            Millisecond,
            /// <summary>
            /// Represents a second
            /// </summary>
            Second,
            /// <summary>
            /// Represents a minute (60 seconds)
            /// </summary>
            Minute,
            /// <summary>
            /// Represents an hour (60 minutes)
            /// </summary>
            Hour,
            /// <summary>
            /// Represents a day (24 hours)
            /// </summary>
            Day,
            /// <summary>
            /// Represents a week (7 days)
            /// </summary>
            Week,
            /// <summary>
            /// Represents a month (30 days)
            /// </summary>
            Month,
            /// <summary>
            /// Represents a year (365.24 days)
            /// </summary>
            Year,
            /// <summary>
            /// Represents a century (100 years)
            /// </summary>
            Century,
            /// <summary>
            /// Represents a millenium (1000 years)
            /// </summary>
            Millenium
        }

        /// <summary>
        /// Gets the appropriate TimeSpan from a given TimeUnit.
        /// </summary>
        public static Dictionary<TimeUnit, TimeSpan> UnitToTime = new Dictionary<TimeUnit, TimeSpan> {
            { TimeUnit.Millisecond, TimeSpan.FromMilliseconds(1)},
            { TimeUnit.Second, TimeSpan.FromSeconds(1)},
            { TimeUnit.Minute, TimeSpan.FromMinutes(1)},
            { TimeUnit.Hour, TimeSpan.FromHours(1)},
            { TimeUnit.Day, TimeSpan.FromDays(1)},
            { TimeUnit.Week, TimeSpan.FromDays(7)},
            { TimeUnit.Month, TimeSpan.FromDays(30)},
            { TimeUnit.Year, TimeSpan.FromDays(365.24)},
            { TimeUnit.Century, TimeSpan.FromDays(36524)},
            { TimeUnit.Millenium, TimeSpan.FromDays(365240)}
        };

        /// <summary>
        /// Searches the list of static time zone abbreviations in <paramref name="LanguageConfiguration"/> to find the closest expressions to <paramref name="Input"/>.
        /// </summary>
        /// <param name="Input">The search term.</param>
        /// <param name="LanguageConfiguration">The global language configuration from which to draw time zone abbreviations.</param>
        /// <returns>A <c>string[]</c> array of time zone abbreviations from <paramref name="LanguageConfiguration"/> which are most similar to <paramref name="Input"/>, sorted by relevance.</returns>

        public static string[] SearchTimeZone(this string Input, LanguageConfiguration LanguageConfiguration)
        {
            Dictionary<string, int> SearchWeight = new();

            foreach (KeyValuePair<string, TimeZoneData> k in LanguageConfiguration.TimeZones)
            {
                int Weight = 0;
                for (int i = 0; i < k.Key.Length; i++)
                {
                    if (i < Input.Length)
                    {
                        if (Input[i] == k.Key[i]) Weight += 10;
                        else if (char.ToUpper(Input[i]) == char.ToUpper(k.Key[i])) Weight += 9;
                    }
                    if (Input.Contains(k.Key[i])) Weight += 3;
                }

                if (Input.Length >= 2 && Input[^2] == 'S')
                {
                    if (k.Key.Length >= 2 && k.Key[^2] == 'D') Weight += 8;
                    else if (k.Key.Length + 1 == Input.Length) Weight += 4;
                }

                if (k.Key.Length >= 2 && k.Key[^2] == 'S')
                {
                    if (Input.Length >= 2 && Input[^2] == 'D') Weight += 8;
                    else if (Input.Length + 1 == k.Key.Length) Weight += 4;
                }

                if (Input.ToUpper() == k.Key.ToUpper()) Weight += 100;

                SearchWeight.Add(k.Key, Weight);
            }

            List<KeyValuePair<string, int>> WeightedList = SearchWeight.ToList();
            WeightedList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            string[] SortedResults = new string[WeightedList.Count];
            for (int i = 0; i < WeightedList.Count; i++)
            {
                SortedResults[i] = WeightedList[i].Key;
            }
            return SortedResults;
        }

        /// <summary>
        /// Searches the list of static time zone data in <paramref name="LanguageConfiguration"/> to find timezones whose offset is closest to <paramref name="Offset"/>.
        /// </summary>
        /// <param name="Offset">A TimeSpan representing the difference between UTC and a given time zone.</param>
        /// <param name="LanguageConfiguration">The configuration required to parse time zone data.</param>
        /// <param name="ExactMatches">The number of matches in the results that have the exact <paramref name="Offset"/> provided.</param>
        /// <returns>A <c>string[]</c> array of time zone abbreviations from <paramref name="LanguageConfiguration"/> which are most similar in offset to <paramref name="Offset"/>, sorted by relevance.</returns>

        public static string[] SearchTimeZone(this TimeSpan Offset, LanguageConfiguration LanguageConfiguration, out int ExactMatches)
        {
            Dictionary<string, int> SearchWeight = new();
            ExactMatches = 0;

            foreach (KeyValuePair<string, TimeZoneData> k in LanguageConfiguration.TimeZones)
            {
                int Weight = (int)Math.Abs(k.Value.TimeOffset.Subtract(Offset).TotalMinutes);

                if (Weight == 0) ExactMatches++;

                SearchWeight.Add(k.Key, Weight);
            }

            List<KeyValuePair<string, int>> WeightedList = SearchWeight.ToList();
            WeightedList.Sort((pair1, pair2) =>
            {
                if (pair1.Value.CompareTo(pair2.Value) != 0) return pair1.Value.CompareTo(pair2.Value);
                else return pair1.Key.CompareTo(pair2.Key);
            });

            string[] SortedResults = new string[WeightedList.Count];
            for (int i = 0; i < WeightedList.Count; i++)
            {
                SortedResults[i] = WeightedList[i].Key;
            }
            return SortedResults;
        }

        /// <summary>
        /// Finds the longest common substring between <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first string.</param>
        /// <param name="b">The second string.</param>
        /// <returns>The length of the longest common substring found.</returns>

        public static int LongestCommonSubstr(string a, string b)
        {
            return LongestCommonSubstr(a, b, a.Length, b.Length);
        }

        private static int LongestCommonSubstr(string a, string b, int aimax, int bimax)
        {
            int[,] LCSuffix = new int[aimax + 1, bimax + 1];
            int result = 0;

            for (int i = 0; i <= aimax; i++)
            {
                for (int j = 0; j <= bimax; j++)
                {
                    if (i == 0 || j == 0)
                        LCSuffix[i, j] = 0;
                    else if (a[i - 1] == b[j - 1])
                    {
                        LCSuffix[i, j] = LCSuffix[i - 1, j - 1] + 1;

                        if (LCSuffix[i, j] > result) result = LCSuffix[i, j];
                    }
                    else
                        LCSuffix[i, j] = 0;
                }
            }

            return result;
        }

        private const double LengthWeight = 0.015;
        private const double MaxSubstringWeight = 0.9;
        private const double PositionalCorrelationWeight = 0.05;
        private const double CountCorrelationWeight = 0.035;

        /// <summary>
        /// Obtains an index detailing how closely related <paramref name="a"/> and <paramref name="b"/> are based on a series of parameters.
        /// </summary>
        /// <param name="a">The first string to compare</param>
        /// <param name="b">The second string to compare</param>
        /// <returns>A decimal number in the range [0..1] indicating how closely correlated <paramref name="a"/> and <paramref name="b"/> are.</returns>

        public static double GetCorrelationIndex(string a, string b)
        {
            double n = Math.Max(a.Length, b.Length);

            double pLength = Math.Min(a.Length, b.Length) / n;
            int LCSS = LongestCommonSubstr(a, b);
            double pMaxSubstr = LCSS * LCSS / n;
            if (pMaxSubstr > 0.9)
            {
                pMaxSubstr = 0.9 + 0.1 * (LCSS / n);
            }

            Dictionary<char, PairwiseCounter> counts = new Dictionary<char, PairwiseCounter>();
            for (int i = 0; i < a.Length; i++)
            {
                if (!counts.ContainsKey(a[i]))
                {
                    counts.Add(a[i], new PairwiseCounter());
                }
                counts[a[i]].count1++;
            }
            for (int j = 0; j < b.Length; j++)
            {
                if (!counts.ContainsKey(b[j]))
                {
                    counts.Add(b[j], new PairwiseCounter());
                }
                counts[b[j]].count2++;
            }
            double pCounts = 0;
            foreach (PairwiseCounter c in counts.Values)
            {
                pCounts += c.GetCorrelationStrength();
            }
            pCounts /= counts.Count;

            int posCount = 0;
            for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                if (a[i] == b[i])
                    posCount++;
            }
            double pPos = posCount / n;

            return pLength * LengthWeight + pMaxSubstr * MaxSubstringWeight + pPos * PositionalCorrelationWeight + pCounts * CountCorrelationWeight;
        }

        internal class PairwiseCounter
        {
            public int count1 = 0;
            public int count2 = 0;
            public double GetCorrelationStrength()
            {
                return Math.Min(count1, count2) / (double)Math.Max(count1, count2);
            }

            public PairwiseCounter(int count1 = 0, int count2 = 0)
            {
                this.count1 = count1;
                this.count2 = count2;
            }
        }

        /// <summary>
        /// Creates a standard expression of a specific time, both absolute and relative to present.
        /// </summary>
        /// <param name="Time">The DateTimeOffset object to parse.</param>
        /// <param name="BotConfiguration">The Configuration file holding the StandardTimeZone variable, only required if <paramref name="StandardizeTime"/> is <see langword="true"/>.</param>
        /// <param name="StandardizeTime">Whether to standardize the time to <paramref name="BotConfiguration"/><c>.StandardTimeZone</c>.</param>
        /// <returns>A stringified expression of <paramref name="Time"/>.</returns>

        public static string HumanizeExtended(this DateTimeOffset Time, BotConfiguration BotConfiguration = null, bool StandardizeTime = false)
        {
            if (BotConfiguration != null && StandardizeTime)
            {
                Time = Time.ToOffset(TimeSpan.FromHours(BotConfiguration.StandardTimeZone));
            }

            return $"{Time:ddd dd MMM yyy 'at' hh:mm tt 'UTC'zzz} ({Time.Humanize()})";
        }

        /// <summary>
        /// Disables all here, everyone, and role mentions from a given message string, keeping the message mostly identical.
        /// </summary>
        /// <param name="input">The base string to sanitize.</param>
        /// <returns>A modified string where all instances of mass mentions have a zero-width space inserted after the @ symbol to disable them.</returns>

        public static string SanitizeMentions(this string input)
        {
            input = Regex.Replace(input, @"@here", $"@{ZWSP}here", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"@everyone", $"@{ZWSP}everyone", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"<@&", $"<@{ZWSP}&");
            return input;
        }

        /// <summary>
        /// A zero-width space character.
        /// </summary>

        public const char ZWSP = '​';
    }

    /// <summary>
    /// Represents a time zone for comparison as an offset to UTC.
    /// </summary>

    [Serializable]
    public class TimeZoneData
    {

        /// <summary>
        /// The Full name of the time zone.
        /// </summary>

        public string Name { get; set; }

        /// <summary>
        /// The offset to UTC of the time zone, in hours.
        /// </summary>

        public float Offset { get; set; }

        /// <summary>
        /// The offset to UTC of the time zone, as a <c>TimeSpan</c>.
        /// </summary>

        public TimeSpan TimeOffset { get { return TimeSpan.FromMinutes((int)Math.Round(Offset * 60)); } }

        /// <summary>
        /// Stringifies the given timezone
        /// </summary>
        /// <returns>A string expression of the time zone, with critical information.</returns>

        public override string ToString()
        {
            return $"{Name} | {ToTimeZoneExpression(Offset)}";
        }

        /// <summary>
        /// Gives a human-readable form of the <paramref name="Offset"/> relative to UTC.
        /// </summary>
        /// <param name="Offset">The number of hours offset from UTC.</param>
        /// <returns>A string expressing a human-readable form of the offset relative to UTC.</returns>

        public static string ToTimeZoneExpression(float Offset)
        {
            return $"UTC{(Offset >= 0 ? "+" : "")}{(int)Offset}:{Math.Abs(Offset % 1 * 60):00}";
        }

        /// <summary>
        /// Gives a human-readable form of the <paramref name="Offset"/> TimeSpan relative to UTC.
        /// </summary>
        /// <param name="Offset">The TimeSpan object representing the offset of hours relative to UTC.</param>
        /// <returns>A string expressing a human-readable form of the offset relative to UTC.</returns>

        public static string ToTimeZoneExpression(TimeSpan Offset)
        {
            return ToTimeZoneExpression(Offset.Hours + Offset.Minutes / 60f);
        }

        /// <summary>
        /// Attempts to Parse a TimeZone data from given information.
        /// </summary>
        /// <param name="str">The string to parse into TimeZone data.</param>
        /// <param name="languageConfiguration">The Configuration data containing static time zone definitions.</param>
        /// <param name="result">A <c>TimeZoneData</c> object whose name is <paramref name="str"/> or the name attached to the abbreviation and Offset is obtained by parsing <paramref name="str"/></param>
        /// <returns>A <c>TimeZoneData</c> object whose name is <paramref name="str"/> and Offset is obtained by parsing <paramref name="str"/>.</returns>

        public static bool TryParse(string str, LanguageConfiguration languageConfiguration, out TimeZoneData result)
        {

            bool success = false;
            result = new TimeZoneData()
            {
                Offset = 0,
                Name = str
            };

            if (str is null)
            {
                return false;
            }

            int sign = 1;

            int signPos = str.IndexOf("+");
            if (signPos < 0)
            {
                sign = -1;
                signPos = str.IndexOf("-");
            }

            string TZString = signPos < 0 ? str : str[..signPos];
            if (!string.IsNullOrEmpty(TZString))
            {
                if (LanguageHelper.TryParseTimeZone(TZString.Trim(), languageConfiguration, out TimeZoneData TimeZone))
                {
                    result.Name = TimeZone.Name;
                    result.Offset = TimeZone.Offset;
                    success = true;
                }
            }
            else
            {
                result.Name = "UTC";
            }

            if (signPos >= 0)
            {
                string[] mods = str[(signPos + 1)..].Split(":");
                result.Name += str[signPos] + str[(signPos + 1)..];
                result.Offset += int.Parse(mods[0]) * sign;
                if (mods.Length > 1) result.Offset += int.Parse(mods[1]) / 60f;
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Holds regional indicator characters, where 0 is 🇦 and 25 is 🇿.
        /// </summary>
        public static readonly Dictionary<int, string> Indicators = new Dictionary<int, string>() {
            {0, "🇦"},
            {1, "🇧"},
            {2, "🇨"},
            {3, "🇩"},
            {4, "🇪"},
            {5, "🇫"},
            {6, "🇬"},
            {7, "🇭"},
            {8, "🇮"},
            {9, "🇯"},
            {10, "🇰"},
            {11, "🇱"},
            {12, "🇲"},
            {13, "🇳"},
            {14, "🇴"},
            {15, "🇵"},
            {16, "🇶"},
            {17, "🇷"},
            {18, "🇸"},
            {19, "🇹"},
            {20, "🇺"},
            {21, "🇻"},
            {22, "🇼"},
            {23, "🇽"},
            {24, "🇾"},
            {25, "🇿"}
        };
    }
}