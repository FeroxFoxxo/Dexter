using Dexter.Attributes;
using Dexter.Enums;
using Discord.Commands;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("topic")]
        [Summary("A topic starter command - perfect for when chat has died!\n" +
                    "`ADD [TOPIC]` - adds a topic to the database.\n" +
                    "`GET [TOPIC]` - gets a topic by name from the database.\n" +
                    "`EDIT [TOPIC ID] [TOPIC]` - edits a topic in the database.\n" +
                    "`REMOVE [TOPIC ID]` - removes a topic from the database.")]

        public async Task TopicCommand([Optional] [BotChannelParameter] ActionType ActionType, [Optional] [Remainder] [BotChannelParameter] string Topic) {
            switch (ActionType) {
                case ActionType.Add:
                    await AddTopic(Topic, TopicType.Topic);
                    break;
                case ActionType.Get:
                    await GetTopic(Topic, TopicType.Topic);
                    break;
                case ActionType.Remove:
                    if (int.TryParse(Topic, out int TopicID))
                        await RemoveTopic(TopicID, TopicType.Topic);
                    else
                        throw new Exception("No topic ID provided! To use this command please use the syntax of `remove [TOPIC ID]`.");
                    break;
                case ActionType.Edit:
                    if (int.TryParse(Topic.Split(' ')[0], out int EditTopicID))
                        await EditTopic(EditTopicID, string.Join(' ', Topic.Split(' ').Skip(1).ToArray()), TopicType.Topic);
                    else
                        throw new Exception("No topic ID provided! To use this command please use the syntax of `edit [TOPIC ID] [EDITED TOPIC]`.");
                    break;
                case ActionType.Unknown:
                    await SendTopic(TopicType.Topic);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(ActionType.ToString());
            }

        }

    }

}
