using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public class FunCommands : AbstractModule {
        private readonly FunConfiguration FunConfiguration;

        public FunCommands(BotConfiguration _BotConfiguration, FunConfiguration _FunConfiguration) : base(_BotConfiguration) {
            FunConfiguration = _FunConfiguration;
        }

        [Command("nuzzle")]
        [Summary("Nuzzles a mentioned user or yourself.")]
        public async Task NuzzleCommand([Optional] IGuildUser User) {
            if (User == null)
                User = (IGuildUser)Context.User;

            await Context.Channel.SendMessageAsync($"*nuzzles {User.Mention} floofily*");
        }

        [Command("say")]
        [Summary("I now have a voice! Use the ~say command so speak *through* me!")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task SayCommand([Remainder] string Message) {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(Message);
        }

        [Command("gay")]
        [Summary("How gay are you? Use this command to find out-")]
        public async Task GayCommand([Optional] IGuildUser User) {
            if (User == null)
                User = (IGuildUser)Context.User;

            int Percentage = new Random((User.Id / new DateTime(1970, 1, 1).Subtract(DateTime.Now).TotalDays).ToString().GetHashCode()).Next(102);

            await Context.Channel.SendMessageAsync($"**{User.Username}'s** level of gay is {(Percentage > 100 ? "***over 9000!***" : $"**{Percentage}%**")}. "
                + $"{(User.Id == Context.Message.Author.Id ? "You're" : User.Id == Context.Client.CurrentUser.Id ? "I'm" : "They're")} **{(Percentage < 33 ? "heterosexual" : Percentage < 66 ? "bisexual" : "homosexual")}**! "
                + Emote.Parse(FunConfiguration.EmojiIDs[Percentage < 33 ? "annoyed" : Percentage < 66 ? "wut" : "love"]));
        }

        [Command("8ball")]
        [Summary("Ask the Magic 8-Ball a question and it'll reach into the future to find the answers-")]
        public async Task EightBallCommand([Remainder] string Message) {
            string Result = new Random().Next(4) == 3 ? "uncertain" : new Random(Message.GetHashCode()).Next(2) == 0 ? "yes" : "no";

            string[] Responces = FunConfiguration.EightBall[Result];

            Emote emoji = Emote.Parse(FunConfiguration.EmojiIDs[FunConfiguration.EightBallEmoji[Result]]);

            await Context.Channel.SendMessageAsync($"{Responces[new Random().Next(Responces.Length)]}, **{Context.Message.Author}** {emoji}");
        }

        [Command("topic")]
        [Summary("A topic starter command - perfect for when chat has died!")]
        public async Task TopicCommand() {
            await Question("Topic", FunConfiguration.Topic);
        }

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from.")]
        public async Task WYRCommand() {
            await Question("Would-You-Rather", FunConfiguration.WouldYouRather);
        }

        public async Task Question(string Type, Dictionary<string, string[]> Dictionary) {
            List<KeyValuePair<string, string>> NameQuestionDictionary = new List<KeyValuePair<string, string>>();

            foreach (KeyValuePair<string, string[]> PairsOfQuestions in Dictionary)
                foreach (string PairedWYR in PairsOfQuestions.Value)
                    NameQuestionDictionary.Add(new KeyValuePair<string, string>(PairsOfQuestions.Key, PairedWYR));

            KeyValuePair<string, string> Question = NameQuestionDictionary[new Random().Next(NameQuestionDictionary.Count)];

            await BuildEmbed(EmojiEnum.Sign)
                .WithAuthor(Context.Message.Author)
                .WithTitle($"{BotConfiguration.Bot_Name} Asks")
                .WithDescription(Question.Value)
                .WithFooter($"{Type} Written by {Question.Key}")
                .SendEmbed(Context.Channel);
        }
    }
}
