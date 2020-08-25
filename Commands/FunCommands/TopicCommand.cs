using Dexter.Core.Abstractions;
using Dexter.Core.DiscordApp;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands {

        [Command("topic")]
        [Summary("A topic starter command - perfect for when chat has died!")]

        public async Task TopicCommand() {
            List<KeyValuePair<string, string>> NameQuestionDictionary = new List<KeyValuePair<string, string>>();

            foreach (KeyValuePair<string, string[]> PairsOfQuestions in FunConfiguration.Topic)
                foreach (string PairedWYR in PairsOfQuestions.Value)
                    NameQuestionDictionary.Add(new KeyValuePair<string, string>(PairsOfQuestions.Key, PairedWYR));

            KeyValuePair<string, string> Question = NameQuestionDictionary[new Random().Next(NameQuestionDictionary.Count)];

            await Context.BuildEmbed(EmojiEnum.Sign)
                .WithAuthor(Context.Message.Author)
                .WithTitle($"{Context.BotConfiguration.Bot_Name} Asks")
                .WithDescription(Question.Value)
                .WithFooter($"Topic Written by {Question.Key}")
                .SendEmbed(Context.Channel);
        }

    }
}
