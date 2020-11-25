using Dexter.Attributes;
using Discord;
using Discord.Commands;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class FunCommands {

        [Command("gay")]
        [Summary("How gay are you? Use this command to find out~!")]
        [Alias("straight", "bisexual")]
        [BotChannel]
        [CommandCooldown(60)]

        public async Task GayCommand([Optional] IUser User) {
            if (User == null)
                User = Context.Message.Author;

            int Percentage = new Random((User.Id / DateTime.UnixEpoch.Subtract(DateTime.UtcNow).TotalDays).ToString().GetHashCode()).Next(102);

            await Context.Channel.SendMessageAsync($"**{User.Username}'s** level of gay is {(Percentage > 100 ? "***over 9000!***" : $"**{Percentage}%**")}. "
                + $"{(User.Id == Context.Message.Author.Id ? "You're" : User.Id == Context.Client.CurrentUser.Id ? "I'm" : "They're")} **{(Percentage < 33 ? "heterosexual" : Percentage < 66 ? "bisexual" : "homosexual")}**! "
                + await DiscordSocketClient.GetGuild(FunConfiguration.EmojiGuildID).GetEmoteAsync(FunConfiguration.EmojiIDs[Percentage < 33 ? "annoyed" : Percentage < 66 ? "wut" : "love"]));
        }

    }
}
