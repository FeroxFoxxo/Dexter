using System;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Discord.Commands;

namespace Dexter.Commands {

    public partial class FunCommands {

        /// <summary>
        /// Functions as an interface between the functions in this module and the JSON configuration file holding data for this module.
        /// </summary>

        public Configurations.LanguageConfiguration LanguageConfiguration { get; set; }

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

        private string GeneratePrompt() {
            Random RNG = new Random();

            string Opening = FunConfiguration.WritingPromptOpenings[RNG.Next(FunConfiguration.WritingPromptOpenings.Count)];
            string Predicate = Helpers.LanguageHelper.RandomizePredicate(FunConfiguration.WritingPromptPredicates[RNG.Next(FunConfiguration.WritingPromptPredicates.Count)],
                FunConfiguration.WritingPromptTerms,
                RNG,
                LanguageConfiguration);

            return $"{Opening} {Predicate}";
        }
    }
}
