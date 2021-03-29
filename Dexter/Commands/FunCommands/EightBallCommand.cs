using Dexter.Attributes.Methods;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        /// <summary>
        /// Sends a reply from a predetermined list in a random fashion. The reply can be affirmative, negative, or hazy.
        /// </summary>
        /// <param name="Message">The text corresponding to the asked question.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("8ball")]
        [Summary("Ask the Magic 8-Ball a question and it'll reach into the future to find the answers-")]
        [Alias("8-ball")]
        [CommandCooldown(60)]

        public async Task EightBallCommand([Remainder] string Message) {
            string Result = new Random().Next(4) == 3 ? "uncertain" : new Random(Message.GetHash()).Next(2) == 0 ? "yes" : "no";

            string[] Responses = FunConfiguration.EightBall[Result];

            Emote Emoji = await DiscordSocketClient.GetGuild(FunConfiguration.EmojiGuildID).GetEmoteAsync(FunConfiguration.EmojiIDs[FunConfiguration.EightBallEmoji[Result]]);

            await Context.Channel.SendMessageAsync($"{Responses[new Random().Next(Responses.Length)]}, **{Context.User}** {Emoji}");
        }

    }

}
