using Dexter.Attributes;
using Dexter.Enums;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from.")]
        [Alias("would you rather", "wouldyourather")]
        [CommandCooldown(240)]

        public async Task WYRCommand() => await SendTopic(TopicType.WouldYouRather);

        [Command("wyr")]
        [Summary("A way to ADD or GET a wyr to/fro the database. Takes in a topic or ID as a parameter.")]
        [Alias("would you rather", "wouldyourather")]

        public async Task WYRCommand(CMDActionType CMDActionType, [Remainder] string WYR) {
            switch (CMDActionType) {
                case CMDActionType.Add:
                    await AddTopic(WYR, TopicType.WouldYouRather);
                    break;
                case CMDActionType.Get:
                    await GetTopic(WYR, TopicType.WouldYouRather);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }

        }

        [Command("wyr")]
        [Summary("A way to REMOVE a wyr from the database. Takes in a wyr's ID as a parameter, which can be gotten from the GET command.")]

        public async Task WYRCommand(CMDActionType CMDActionType, int TopicID) {

            switch (CMDActionType) {
                case CMDActionType.Remove:
                    await RemoveTopic(TopicID, TopicType.WouldYouRather);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(CMDActionType.ToString());
            }

        }

        [Command("wyr")]
        [Summary("A way to EDIT a wyr in the database. " +
            "Takes in the wyr's ID as a parameter, which can be gotten from the GET command, and the edited wyr.")]

        public async Task WYRCommand(CMDActionType CMDActionType, int TopicID, [Remainder] string EditedTopic) {

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
