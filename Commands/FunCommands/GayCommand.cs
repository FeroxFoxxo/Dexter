using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("gay")]
        [Summary("How gay are you? Use this command to find out-")]
        [Alias("straight", "bisexual", "bi-sexual", "bi sexual")]
        public async Task GayCommand() {
            await GayCommand(Context.Guild.GetUser(Context.User.Id));
        }

        [Command("gay")]
        [Summary("How gay are you? Use this command to find out-")]
        [Alias("straight", "bisexual", "bi-sexual", "bi sexual")]
        public async Task GayCommand(IGuildUser User) {
            int Percentage = new Random((User.Id / new DateTime(1970, 1, 1).Subtract(DateTime.Now).TotalDays).ToString().GetHashCode()).Next(102);

            await Context.Channel.SendMessageAsync($"**{User.Username}'s** level of gay is {(Percentage > 100 ? "***over 9000!***" : $"**{Percentage}%**")}. "
                + $"{(User.Id == Context.Message.Author.Id ? "You're" : User.Id == Context.Client.CurrentUser.Id ? "I'm" : "They're")} **{(Percentage < 33 ? "heterosexual" : Percentage < 66 ? "bisexual" : "homosexual")}**! "
                + Emote.Parse(FunConfiguration.EmojiIDs[Percentage < 33 ? "annoyed" : Percentage < 66 ? "wut" : "love"]));
        }

    }
}
