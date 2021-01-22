using System;
using System.Collections.Generic;
using System.Text;

namespace Dexter.Commands {

    /// <summary>
    /// Holds a variety of tools to deal with organic string management/manipulation.
    /// </summary>

    public static class LanguageHelper {

        private enum ArticleType {
            None,
            Uppercase,
            Lowercase
        }

        /// <summary>
        /// Randomizes special groups of characters in <paramref name="Predicate"/> and fills them in with corresponding terms in <paramref name="TermBanks"/>.
        /// </summary>
        /// <remarks><para>The way to mark an expression for randomization is to wrap it in braces ("{}"). The format of each expression must be as follows: {IDENTIFIERX}, where IDENTIFIER is a key in <paramref name="TermBanks"/> and X is a positive integer value, every expression with the same identifier and value will be swapped for the same term.</para>
        /// <para>Special Identifiers exist, which do not run through terms: 'a' will try to guess the most probable expression of the indefinite article.</para></remarks>
        /// <param name="Predicate">The message whose terms are to be randomized.</param>
        /// <param name="TermBanks">A string-string[] dictionary where the keys are the explicit identifier of each TermClass and the values are list of terms those expressions can be substituted with.</param>
        /// <param name="RNG">A Random Number Generator used to extract random terms from <paramref name="TermBanks"/>.</param>
        /// <returns>A <c>string</c> holding the new, randomized predicate.</returns>

        public static string RandomizePredicate(string Predicate, Dictionary<string, string[]> TermBanks, Random RNG) {
            HashSet<TermClass> Terms = new HashSet<TermClass>();

            foreach (KeyValuePair<string, string[]> k in TermBanks) {
                Terms.Add(new TermClass(k.Key, k.Value));
            }

            StringBuilder NewPredicate = new StringBuilder(Predicate.Length * 2);

            ArticleType ResolveArticle = ArticleType.None;

            while (Predicate.Length > 0) {
                int InsertIndex = Predicate.IndexOf(INSERTION_INDICATOR);
                if (InsertIndex == -1) {
                    NewPredicate.Append(Predicate);
                    break;
                }

                int EndIndex = Predicate.IndexOf(INSERTION_END);
                if (EndIndex == -1) {
                    throw new FormatException($"There was an error parsing predicate {Predicate}, unbalanced braces. Please contact the developer team.");
                }

                NewPredicate.Append(Predicate[..InsertIndex]);

                string CompareString = Predicate[(InsertIndex + 1)..EndIndex];

                //SPECIAL CASES
                if (CompareString is "a" or "A") {
                    ResolveArticle = CompareString == "a" ? ArticleType.Lowercase : ArticleType.Uppercase;
                } else { 
                    //Default case
                    foreach (TermClass Term in Terms) {
                        int Index = Term.CheckReplace(CompareString);

                        if (Index == ERROR_CODE_INVALID_NUMBER)
                            throw new IndexOutOfRangeException($"There was an error parsing the number in term call \"{CompareString}\" within \"{Predicate}\". Please contact the developer team.");

                        if (Index >= 0) {
                            string newTerm = Term.GetOrGenerateCached(Index, RNG);
                            
                            NewPredicate.Append((ResolveArticle != ArticleType.None ? $"{GuessIndefiniteArticle(newTerm, ResolveArticle == ArticleType.Uppercase)} " : "") 
                                + newTerm);
                            ResolveArticle = ArticleType.None;
                            break;
                        }
                    }
                }

                Predicate = Predicate[(EndIndex + 1)..];
            }

            return NewPredicate.ToString();
        }

        const char INSERTION_INDICATOR = '{';
        const char INSERTION_END = '}';

        const int ERROR_CODE_INVALID_NUMBER = 422;

        /// <summary>
        /// Checks whether an "IDENTIFIERX" expression shares an identifier with <paramref name="Term"/>, and returns the index X.
        /// </summary>
        /// <param name="Str">The raw form of the identifier-index expression, without braces.</param>
        /// <param name="Term">The TermClass to compare <paramref name="Str"/> against.</param>
        /// <returns>The number accompanying the raw expression <paramref name="Str"/> if their identifiers match, Invalid Number Error Code if the term X can't be parsed to a positive integer, -1 otherwise.</returns>

        private static int CheckReplace(this TermClass Term, string Str) {
            if (!Str.StartsWith(Term.Identifier))
                return -1;

            if (int.TryParse(Str[Term.Identifier.Length..], out int Index)) {
                return Index >= 0 ? Index : ERROR_CODE_INVALID_NUMBER;
            }

            return ERROR_CODE_INVALID_NUMBER;
        }

        /// <summary>
        /// Holds a list of terms related by a common Identifier and provides a way to generate random terms.
        /// </summary>

        internal class TermClass {

            private const int MAX_REPLACE_ATTEMPTS = 5;
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
            /// <param name="MaxRetries">The maximum amount of attempts to generate a novel term from the bank.</param>
            /// <param name="Generator">A random number generator.</param>
            /// <returns>The term at position <paramref name="Index"/> within the Cache.</returns>

            public string GetOrGenerateCached(int Index, Random Generator, int MaxRetries = MAX_REPLACE_ATTEMPTS) {
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

            switch (Relevant[0]) {

                case 'a':
                case 'e':
                case 'i':
                case 'o':
                    return Capitalize ? "An" : "an";
                case 'h':
                case 'u':
                    return Capitalize ? "A(n)" : "a(n)";
                default:
                    return Capitalize ? "A" : "a";
            }
        }
    }
}