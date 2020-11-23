using Dexter.Enums;
using Discord.Commands;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("topic")]
        [Summary("A topic starter command - perfect for when chat has died! " +
                    "Use the add [TOPIC] command to add a topic to the database. " +
                    "Use the get [TOPIC] command to get a topic by name from the database. " +
                    "Use the edit [TOPIC ID] [TOPIC] command to edit a topic in the database. " +
                    "Use the remove [TOPIC ID] command to remove a topic from the database.")]

        public async Task TopicCommand([Optional] ActionType CMDActionType, [Optional][Remainder] string Topic) {
            switch (CMDActionType) {
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
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }

        }

    }

}
