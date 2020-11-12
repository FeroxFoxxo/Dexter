using Dexter.Databases.FunTopics;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class FunCommands {

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from.")]
        [Alias("would you rather", "would_you_rather", "wouldyourather")]

        public async Task WYRCommand() {
            FunTopic Topic = await FunTopicsDB.WouldYouRather.GetRandomTopic();

            await BuildEmbed(EmojiEnum.Sign)
                .WithAuthor(Context.Message.Author)
                .WithTitle($"{Context.Client.CurrentUser.Username} Asks")
                .WithDescription(Topic.Topic)
                .WithFooter($"Would You Rather Written by {Client.GetUser(Topic.ProposerID).Username}")
                .SendEmbed(Context.Channel);
        }

    }
}
