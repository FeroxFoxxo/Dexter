using Dexter.Core;
using Discord.Commands;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands.Services {
    public class FunCommands : Module {
        [Command("8ball")]
        public async Task EightBallCommand([Remainder] string message) {
            message = Regex.Replace(message.ToLower(), "/[^a-zA-Z ]/g", "");

            string result = new Random().Next(4) == 3 ? "uncertain" : new Random(message.GetHashCode()).Next(2) == 0 ? "yes" : "no";

            await ReplyAsync("My answer is " + result);
        }
    }
}
