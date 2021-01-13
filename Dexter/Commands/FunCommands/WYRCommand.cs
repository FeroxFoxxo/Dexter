using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from." +
                    "`ADD [WYR]` - adds a wyr to the database.\n" +
                    "`GET [WYR]` - gets a wyr by name from the database.\n" +
                    "`EDIT [WYR ID] [WYR]` - edits a wyr in the database.\n" +
                    "`REMOVE [WYR ID]` - removes a wyr from the database.")]
        [Alias("would you rather", "wouldyourather")]
        [CommandCooldown(120)]

        public async Task WYRCommand([Optional] ActionType ActionType, [Remainder] string Topic) {
            switch (ActionType) {
                case ActionType.Add:
                    await AddTopic(Topic, TopicType.WouldYouRather);
                    break;
                case ActionType.Get:
                    await GetTopic(Topic, TopicType.WouldYouRather);
                    break;
                case ActionType.Remove:
                    if (int.TryParse(Topic, out int TopicID))
                        await RemoveTopic(TopicID, TopicType.WouldYouRather);
                    else
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Error Removing WYR.")
                            .WithDescription("No would you rather ID provided! To use this command please use the syntax of `remove [WYR ID]`.")
                            .SendEmbed(Context.Channel);
                    break;
                case ActionType.Edit:
                    if (int.TryParse(Topic.Split(' ')[0], out int EditTopicID))
                        await EditTopic(EditTopicID, string.Join(' ', Topic.Split(' ').Skip(1).ToArray()), TopicType.WouldYouRather);
                    else
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Error Editing WYR.")
                            .WithDescription("No would you rather ID provided! To use this command please use the syntax of `edit [TOPIC ID] [EDITED TOPIC]`.")
                            .SendEmbed(Context.Channel);
                    break;
                case ActionType.Unknown:
                    await SendTopic(TopicType.WouldYouRather);
                    break;
                default:
                    await SendTopic(TopicType.WouldYouRather);
                    break;
            }
        }

    }

}
