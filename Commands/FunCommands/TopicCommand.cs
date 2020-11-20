using Dexter.Attributes;
using Dexter.Enums;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("topic")]
        [Summary("A topic starter command - perfect for when chat has died!")]
        [CommandCooldown(240)]

        public async Task TopicCommand() => await SendTopic(TopicType.Topic);

        [Command("topic")]
        [Summary("A way to ADD or GET a topic to/fro the database. Takes in a topic as a parameter.")]

        public async Task TopicCommand(CMDActionType CMDActionType, [Remainder] string Topic) {

            switch(CMDActionType) {
                case CMDActionType.Add:
                    await AddTopic(Topic, TopicType.Topic);
                    break;
                case CMDActionType.Get:
                    await GetTopic(Topic, TopicType.Topic);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }

        }

        [Command("topic")]
        [Summary("A way to REMOVE a topic from the database. Takes in a topic's ID as a parameter, which can be gotten from the GET command.")]

        public async Task TopicCommand(CMDActionType CMDActionType, int TopicID) {

            switch (CMDActionType) {
                case CMDActionType.Remove:
                    await RemoveTopic(TopicID, TopicType.Topic);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }

        }

        [Command("topic")]
        [Summary("A way to EDIT a topic in the database. " +
            "Takes in the topic's ID as a parameter, which can be gotten from the GET command, and the edited topic.")]

        public async Task TopicCommand(CMDActionType CMDActionType, int TopicID, [Remainder] string EditedTopic) {

            switch (CMDActionType) {
                case CMDActionType.Edit:
                    await EditTopic(TopicID, EditedTopic, TopicType.Topic);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }

        }

    }

}
