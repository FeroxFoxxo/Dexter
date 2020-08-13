using Dexter.Core;
using Discord.Commands;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands.Services {
    public class FunCommands : AbstractModule {
        [Command("8ball")]
        public async Task EightBallCommand([Remainder] string Message) {
            Message = Regex.Replace(Message.ToLower(), "/[^a-zA-Z ]/g", "");

            string Result = new Random().Next(4) == 3 ? "uncertain" : new Random(Message.GetHashCode()).Next(2) == 0 ? "yes" : "no";

            await ReplyAsync("My answer is " + Result);
        }
    }
}
