using Dexter.Attributes.Methods;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        /// <summary>
        /// Returns a random percentage measurement that changes every so often depending on the user's ID along with time parameters.
        /// </summary>
        /// <param name="User">The user to make the measurement about.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("gay")]
        [Summary("How gay are you? Use this command to find out~!")]
        [Alias("straight", "bisexual")]
        [BotChannel]

        public async Task GayCommand([Optional] IUser User) {
            if (User == null)
                User = Context.User;

            int Percentage = new Random((User.Id / Math.Round(DateTime.UnixEpoch.Subtract(DateTime.UtcNow).TotalDays)).ToString().GetHash()).Next(102);

            await Context.Channel.SendMessageAsync($"**{User.Username}'s** level of gay is {(Percentage > 100 ? "***over 9000!***" : $"**{Percentage}%**")}. "
                + $"{(User.Id == Context.User.Id ? "You're" : User.Id == Context.Client.CurrentUser.Id ? "I'm" : "They're")} **{(Percentage < 33 ? "heterosexual" : Percentage < 66 ? "bisexual" : "homosexual")}**! "
                + await DiscordSocketClient.GetGuild(FunConfiguration.EmojiGuildID).GetEmoteAsync(FunConfiguration.EmojiIDs[Percentage < 33 ? "annoyed" : Percentage < 66 ? "wut" : "love"]));
        }

    }

}
