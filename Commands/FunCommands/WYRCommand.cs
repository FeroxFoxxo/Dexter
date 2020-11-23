using Dexter.Enums;
using Discord.Commands;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from. " +
                    "Use the add [WYR] command to add a wyr to the database. " +
                    "Use the get [WYR] command to get a wyr by name from the database. " +
                    "Use the edit [WYR ID] [WYR] command to edit a wyr in the database. " +
                    "Use the remove [WYR ID] command to remove a wyr from the database.")]
        [Alias("would you rather", "wouldyourather")]

        public async Task WYRCommand([Optional] ActionType CMDActionType, [Optional][Remainder] string Topic) {
            switch (CMDActionType) {
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
                        throw new Exception("No wyr ID provided! To use this command please use the syntax of `remove [WYR ID]`.");
                    break;
                case ActionType.Edit:
                    if (int.TryParse(Topic.Split(' ')[0], out int EditTopicID))
                        await EditTopic(EditTopicID, string.Join(' ', Topic.Split(' ').Skip(1).ToArray()), TopicType.WouldYouRather);
                    else
                        throw new Exception("No wyr ID provided! To use this command please use the syntax of `edit [WYR ID] [EDITED WYR]`.");
                    break;
                case ActionType.Unknown:
                    await SendTopic(TopicType.WouldYouRather);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }

        }

    }

}
