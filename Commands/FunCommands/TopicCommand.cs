using Dexter.Databases.FunTopics;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class FunCommands {

        [Command("topic")]
        [Summary("A topic starter command - perfect for when chat has died!")]

        public async Task TopicCommand() {
            FunTopic Topic = await FunTopicsDB.Topics.GetRandomTopic();

            await BuildEmbed(EmojiEnum.Sign)
                .WithAuthor(Context.Message.Author)
                .WithTitle($"{Context.Client.CurrentUser.Username} Asks")
                .WithDescription(Topic.Topic)
                .WithFooter($"Topic Written by {Client.GetUser(Topic.ProposerID).Username}")
                .SendEmbed(Context.Channel);
        }

    }
}
