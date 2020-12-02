using Dexter.Attributes;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class FunCommands {

        [Command("8ball")]
        [Summary("Ask the Magic 8-Ball a question and it'll reach into the future to find the answers-")]
        [Alias("8-ball")]
        [CommandCooldown(60)]

        public async Task EightBallCommand([Remainder] string Message) {
            string Result = new Random().Next(4) == 3 ? "uncertain" : new Random(Message.GetHash()).Next(2) == 0 ? "yes" : "no";

            string[] Responces = FunConfiguration.EightBall[Result];

            Emote Emoji = await DiscordSocketClient.GetGuild(FunConfiguration.EmojiGuildID).GetEmoteAsync(FunConfiguration.EmojiIDs[FunConfiguration.EightBallEmoji[Result]]);

            await Context.Channel.SendMessageAsync($"{Responces[new Random().Next(Responces.Length)]}, **{Context.User}** {Emoji}");
        }

    }
}
