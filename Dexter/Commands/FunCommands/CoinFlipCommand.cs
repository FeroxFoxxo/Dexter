using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class FunCommands {

        /// <summary>
        /// Flips a given number of coins, or 1 if none are provided.
        /// </summary>
        /// <param name="Args">The message after the command, if the first element can be parsed to an <c>int</c>, it will take that as the number of coin flips.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the process completes successfully.</returns>

        [Command("coinflip")]
        [Summary("Flips a coin, or a number of coins if a number is provided.")]
        
        public async Task CoinFlipCommand([Remainder] string Args = null) {
            if(!string.IsNullOrEmpty(Args) && int.TryParse(Args.Split(" ")[0], out int Flips)) {
                if (Flips < 0) Flips = Math.Abs(Flips);
            } else {
                Flips = 1;
            }

            if (Flips == 0) {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Flipped no coins!")
                    .WithDescription("I don't really understand what you are trying to do, but you achieved it!")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (Flips > FunConfiguration.MaxCoinFlips) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("COINS EVERYWHERE!!!")
                    .WithDescription($"I tried to flip them, I really tried... But they're just **too many!** Try with {FunConfiguration.MaxCoinFlips} or less.")
                    .SendEmbed(Context.Channel);
                return;
            }

            Random RNG = new Random();

            bool[] Results = new bool[Flips];

            int Heads = 0;
            int Tails = 0;

            for(int i = 0; i < Flips; i++) {
                bool newResult = RNG.Next(0, 2) == 0;

                Results[i] = newResult;
                if (newResult) Heads++;
                else Tails++;
            }

            if(Flips == 1) {
                await Context.Channel.SendMessageAsync($"**{(Results[0] ? "Heads" : "Tails")}** it is!");
            } else if (Flips < 16) {
                string[] ResultsStr = new string[Flips];

                for(int i = 0; i < Flips; i++) {
                    ResultsStr[i] = Results[i] ? "**Heads**" : "**Tails**";
                }

                await Context.Channel.SendMessageAsync($"Flipped {string.Join(", ", ResultsStr)}! Let me just check no coins fell under the table...");
            } else if (Flips < 100) {
                string[] ResultsStr = new string[Flips];

                for (int i = 0; i < Flips; i++) {
                    ResultsStr[i] = Results[i] ? "H" : "T";
                }

                await Context.Channel.SendMessageAsync($"Flipped **{Heads} Heads** and **{Tails} Tails**!\n{string.Join((Flips < 50 ? "-" : ""), ResultsStr)}!");
            } else {
                await Context.Channel.SendMessageAsync($"Flipped **{Heads} Heads** and **{Tails} Tails**! That was flipping intense!!!");
            }
        }

    }
}
