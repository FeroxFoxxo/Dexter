using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;

namespace Dexter.Commands {

    public partial class FunCommands {

        /// <summary>
        /// Sends a randomly generated writing prompt. It draws from FunConfiguration.WritingPrompt* config items.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("writingprompt")]
        [Summary("Provides a randomly generated writing prompt.")]
        [BotChannel]

        public async Task WritingPromptCommand() {
            await Context.Channel.SendMessageAsync(GeneratePrompt());
        }

        internal static class Language {
            public static string TreatPredicate(string Predicate, Dictionary<string, string[]> TermBanks, Random RNG) {
                HashSet<TermClass> Terms = new HashSet<TermClass>(); 

                foreach(KeyValuePair<string, string[]> k in TermBanks) {
                    Terms.Add(new TermClass(k.Key, k.Value));
                }

                StringBuilder NewPredicate = new StringBuilder(Predicate.Length * 2);

                while(Predicate.Length > 0) {
                    int InsertIndex = Predicate.IndexOf(INSERTION_INDICATOR);
                    if (InsertIndex == -1) {
                        NewPredicate.Append(Predicate);
                        break;
                    }

                    int EndIndex = Predicate.IndexOf(INSERTION_END);
                    if (EndIndex == -1) {
                        return $"There was an error parsing predicate {Predicate}, unbalanced braces. Please contact the developer team.";
                    }

                    NewPredicate.Append(Predicate[..InsertIndex]);

                    string CompareString = Predicate[(InsertIndex+1)..EndIndex];

                    foreach (TermClass Term in Terms) {
                        int Index = CheckReplace(CompareString, Term);

                        if (Index == ERROR_CODE_INVALID_NUMBER)
                            return $"There was an error parsing the number in term call \"{CompareString}\" within \"{Predicate}\"";

                        if (Index >= 0) {
                            NewPredicate.Append(Term.GetOrGenerateCached(Index, RNG));
                            break;
                        }
                    }

                    Predicate = Predicate[(EndIndex + 1)..];
                }

                return NewPredicate.ToString();
            }

            const char INSERTION_INDICATOR = '{';
            const char INSERTION_END = '}';

            const int ERROR_CODE_INVALID_NUMBER = 422;

            private static int CheckReplace(string Str, TermClass Term) {
                if (!Str.StartsWith(Term.Identifier))
                    return -1;

                if (int.TryParse(Str[Term.Identifier.Length..], out int Index))
                    return Index > 0 ? Index : ERROR_CODE_INVALID_NUMBER;

                return ERROR_CODE_INVALID_NUMBER;
            }

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

                public string GetOrGenerateCached(int Index, Random Generator) {
                    while(Cache.Count <= Index) {
                        Cache.Add(Bank[Generator.Next(Bank.Length)]);
                        
                        //If this term is already in the cache, try to replace it.
                        for(int i = 0; i < MAX_REPLACE_ATTEMPTS && Cache.IndexOf(Cache[^1]) < Cache.Count - 1; i++) {
                            Cache[^1] = Bank[Generator.Next(Bank.Length)];
                        }
                    }

                    return Cache[Index];
                }
            }
        }

        private string GeneratePrompt() {
            Random RNG = new Random();

            string Opening = FunConfiguration.WritingPromptOpenings[RNG.Next(FunConfiguration.WritingPromptOpenings.Count)];
            string Predicate = Language.TreatPredicate(FunConfiguration.WritingPromptPredicates[RNG.Next(FunConfiguration.WritingPromptPredicates.Count)],
                FunConfiguration.WritingPromptTerms,
                RNG);

            return $"{Opening} {Predicate}";
        }
    }
}
