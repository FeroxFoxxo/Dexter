using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from.")]
        [Alias("would you rather", "would_you_rather", "wouldyourather")]
        public async Task WYRCommand() {
            List<KeyValuePair<string, string>> NameQuestionDictionary = new List<KeyValuePair<string, string>>();

            foreach (KeyValuePair<string, string[]> PairsOfQuestions in FunConfiguration.WouldYouRather)
                foreach (string PairedWYR in PairsOfQuestions.Value)
                    NameQuestionDictionary.Add(new KeyValuePair<string, string>(PairsOfQuestions.Key, PairedWYR));

            KeyValuePair<string, string> Question = NameQuestionDictionary[new Random().Next(NameQuestionDictionary.Count)];

            await Context.BuildEmbed(EmojiEnum.Sign)
                .WithAuthor(Context.Message.Author)
                .WithTitle($"{Context.BotConfiguration.Bot_Name} Asks")
                .WithDescription(Question.Value)
                .WithFooter($"Would You Rather Written by {Question.Key}")
                .SendEmbed(Context.Channel);
        }

    }
}
