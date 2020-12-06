﻿using Dexter.Attributes.Methods;
using Dexter.Enums;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from.")]
        public async Task WYRCommand() => await SendTopic(TopicType.Topic);

        [Command("wyr")]
        [BotChannel]
        [Summary("`ADD [WYR]` - adds a wyr to the database.\n" +
                    "`GET [WYR]` - gets a wyr by name from the database.\n" +
                    "`EDIT [WYR ID] [WYR]` - edits a wyr in the database.\n" +
                    "`REMOVE [WYR ID]` - removes a wyr from the database.")]
        [Alias("would you rather", "wouldyourather")]

        public async Task WYRCommand(ActionType ActionType, [Remainder] string Topic) {
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
                    throw new ArgumentOutOfRangeException(ActionType.ToString());
            }

        }

    }

}
