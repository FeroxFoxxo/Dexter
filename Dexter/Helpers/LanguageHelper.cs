using System;
using System.Collections.Generic;
using System.Text;
using Dexter.Configurations;

namespace Dexter.Helpers {

    /// <summary>
    /// Holds a variety of tools to deal with organic string management/manipulation.
    /// </summary>

    public static class LanguageHelper {

        private enum ArticleType {
            None,
            Uppercase,
            Lowercase
        }

        private enum PluralType {
            None,
            Plural
        }

        private enum PossessiveType {
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

        public static string RandomizePredicate(string Predicate, Dictionary<string, string[]> TermBanks, Random RNG, LanguageConfiguration Config) {
            HashSet<TermClass> Terms = new HashSet<TermClass>();

            foreach (KeyValuePair<string, string[]> k in TermBanks) {
                Terms.Add(new TermClass(k.Key, k.Value));
            }

            StringBuilder NewPredicate = new StringBuilder(Predicate.Length * 2);

            ArticleType ResolveArticle = ArticleType.None;
            PluralType ResolvePlural = PluralType.None;
            PossessiveType ResolvePossessive = PossessiveType.None;

            while (Predicate.Length > 0) {
                int InsertIndex = Predicate.IndexOf(Config.TermInsertionStartIndicator);
                if (InsertIndex == -1) {
                    NewPredicate.Append(Predicate);
                    break;
                }

                int EndIndex = Predicate.IndexOf(Config.TermInsertionEndIndicator);
                if (EndIndex == -1) {
                    throw new FormatException($"There was an error parsing predicate {Predicate}, unbalanced braces. Please contact the developer team.");
                }

                NewPredicate.Append(Predicate[..InsertIndex]);

                string CompareString = Predicate[(InsertIndex + 1)..EndIndex];

                //SPECIAL CASES
                if (CompareString is "a" or "A") {
                    ResolveArticle = CompareString == "a" ? ArticleType.Lowercase : ArticleType.Uppercase;
                } else if (CompareString.ToLower() == "plural") {
                    ResolvePlural = PluralType.Plural;
                } else if (CompareString.ToLower() == "\'s") {
                    ResolvePossessive = PossessiveType.Possessive;
                } else {
                    //Default case
                    foreach (TermClass Term in Terms) {
                        int Index = Term.CheckReplace(CompareString, Config);

                        if (Index == (int)Config.ErrorCodeInvalidArgument)
                            throw new IndexOutOfRangeException($"There was an error parsing the number in term call \"{CompareString}\" within \"{Predicate}\". Please contact the developer team.");

                        if (Index >= 0) {
                            string newTerm = Term.GetOrGenerateCached(Index, RNG, Config);

                            if (ResolvePlural != PluralType.None) {
                                newTerm = newTerm.GuessPlural(Config);
                            }

                            if (ResolvePossessive != PossessiveType.None) {
                                newTerm = newTerm.Possessive();
                            }

                            if (ResolveArticle != ArticleType.None) {
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

        private static int CheckReplace(this TermClass Term, string Str, LanguageConfiguration Config) {
            if (!Str.StartsWith(Term.Identifier))
                return -1;

            if (int.TryParse(Str[Term.Identifier.Length..], out int Index)) {
                return Index >= 0 ? Index : (int)Config.ErrorCodeInvalidArgument;
            }

            return (int)Config.ErrorCodeInvalidArgument;
        }

        /// <summary>
        /// Holds a list of terms related by a common Identifier and provides a way to generate random terms.
        /// </summary>

        internal class TermClass {

            public readonly string Identifier;
            private readonly string[] Bank;
            private readonly List<string> Cache;

            public TermClass(string Identifier, string[] Bank) {
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

            public string GetOrGenerateCached(int Index, Random Generator, LanguageConfiguration Config, int MaxRetries = -1) {
                if (MaxRetries < 0)
                    MaxRetries = (int)Config.TermRepetitionAversionFactor;

                while (Cache.Count <= Index) {
                    Cache.Add(Bank[Generator.Next(Bank.Length)]);

                    //If this term is already in the cache, try to replace it for a new, random one.
                    for (int i = 0; i < MaxRetries && Cache.IndexOf(Cache[^1]) < Cache.Count - 1; i++) {
                        Cache[^1] = Bank[Generator.Next(Bank.Length)];
                    }
                }

                return Cache[Index];
            }
        }

        /// <summary>
        /// Will attempt to guess whether the indefinite article should be 'a' or 'an' based on <paramref name="NextWord"/>.
        /// </summary>
        /// <param name="NextWord">A string describing what follows the article.</param>
        /// <param name="Capitalize">Whether to capitalize the first letter of the article.</param>
        /// <returns>A string, either "a", "an", or "a(n)", where the character 'a' is capitalized if <paramref name="Capitalize"/> is set to <see langword="true"/>.</returns>

        public static string GuessIndefiniteArticle(string NextWord, bool Capitalize = false) {
            string Relevant = NextWord.Trim().Split(' ')[0].ToLower();

            return (Relevant[0]) switch {
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

        public static string GuessPlural(this string Singular, LanguageConfiguration Config) {
            string LowerSingular = Singular.ToLower();

            if (Config.IrregularPlurals.ContainsKey(LowerSingular)) {
                return Config.IrregularPlurals[LowerSingular].MatchCase(Singular);
            } else if (LowerSingular.EndsWith("on")) {
                return Singular[..^2] + "a";
            } else if (LowerSingular.EndsWith("um")) {
                return Singular[..^2] + "a";
            } else if (LowerSingular.EndsWith("us")) {
                return Singular[..^2] + "i";
            } else if (LowerSingular.EndsWith("is")) {
                return Singular[..^2] + "es";
            } else if (LowerSingular.EndsWith("ex") || LowerSingular.EndsWith("ix")) {
                return Singular[..^2] + "ices";
            } else if (LowerSingular[^1] is 's' or 'z') {
                if (LowerSingular.Length > 2 && LowerSingular[^3].IsConsonant() && LowerSingular[^2].IsVowel() && LowerSingular[^1].IsConsonant()) {
                    return Singular + Singular[^1] + "es";
                } else {
                    return Singular + "es";
                }
            } else if (LowerSingular.EndsWith("fe")) {
                return Singular[..^2] + "ves";
            } else if (LowerSingular.EndsWith("f")) {
                return Singular[..^1] + "ves";
            } else if (LowerSingular.EndsWith("y")) {
                return Singular[^2].IsVowel() ? Singular + "s" : Singular[^1] + "ies";
            } else if (LowerSingular[^1] is 'o' or 'x' || LowerSingular.EndsWith("sh") || LowerSingular.EndsWith("ch")) {
                return Singular + "es";
            }

            return Singular + "s";
        }

        /// <summary>
        /// Limits the given string <paramref name="s"/> to a length <paramref name="MaxLength"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="MaxLength"></param>
        /// <returns>A substring of <paramref name="s"/> ending in "..." if its length is greater than <paramref name="MaxLength"/>, otherwise <paramref name="s"/></returns>

        public static string Truncate(this string s, int MaxLength) {
            if(s.Length > MaxLength) {
                if (MaxLength < 3) return "...";
                return s[..^(MaxLength - 3)] + "...";
            }

            return s;
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

        public static string MatchCase(this string Input, string Case) {
            int Match = Input.Length < Case.Length ? Input.Length : Case.Length;

            StringBuilder SB = new StringBuilder(Input.Length);

            for (int i = 0; i < Match; i++) {
                SB.Append(Input[i].MatchCase(Case[i]));
            }

            if (Match < Input.Length) {
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

        public static char MatchCase(this char Input, char Case) {
            if (char.IsUpper(Case))
                return char.ToUpper(Input);

            return char.ToLower(Input);
        }

        /// <summary>
        /// Obtains the ordinal form of an integer <paramref name="N"/>.
        /// </summary>
        /// <param name="N">The base number to obtain the ordinal from.</param>
        /// <returns>The string "<paramref name="N"/>st", "<paramref name="N"/>nd", "<paramref name="N"/>rd" if any are appropriate, otherwise "<paramref name="N"/>th".</returns>

        public static string Ordinal(this int N) {
            if (N < 0)
                N = -N;

            if (N % 100 / 10 == 1)
                return N + "th";

            return (N % 10) switch {
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

        public static string Possessive(this string Input) {
            return $"{Input}'{(Input.EndsWith('s') ? "" : "s")}";
        }
    }
}