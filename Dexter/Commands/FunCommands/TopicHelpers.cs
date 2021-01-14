using Dexter.Databases.FunTopics;
using Dexter.Enums;
using Dexter.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Humanizer;
using Discord;

namespace Dexter.Commands {

    public partial class FunCommands {

        public async Task RunTopic (string Command, TopicType TopicType) {
            string Name = Regex.Replace(TopicType.ToString(), "([A-Z])([a-z]*)", "$1$2");

            if (!string.IsNullOrEmpty(Command)) {
                if (Enum.TryParse(Command.Split(" ")[0].ToLower().Pascalize(), out Enums.ActionType ActionType)) {
                    string Topic = Command[(Command.Split(" ")[0].Length + 1)..];

                    switch (ActionType) {
                        case Enums.ActionType.Add:
                            await AddTopic(Topic, TopicType);
                            break;
                        case Enums.ActionType.Get:
                            await GetTopic(Topic, TopicType);
                            break;
                        case Enums.ActionType.Remove:
                            if (int.TryParse(Topic, out int TopicID))
                                await RemoveTopic(TopicID, TopicType);
                            else
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle($"Error Removing {Name}.")
                                    .WithDescription($"No {Name.ToLower()} ID provided! To use this command please use the syntax of `remove [ID]`.")
                                    .SendEmbed(Context.Channel);
                            break;
                        case Enums.ActionType.Edit:
                            if (int.TryParse(Topic.Split(' ')[0], out int EditTopicID))
                                await EditTopic(EditTopicID, string.Join(' ', Topic.Split(' ').Skip(1).ToArray()), TopicType);
                            else
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle($"Error Editing {Name}.")
                                    .WithDescription($"No {Name.ToLower()} ID provided! To use this command please use the syntax of `edit [ID] [TOPIC]`.")
                                    .SendEmbed(Context.Channel);
                            break;
                        case Enums.ActionType.Unknown:
                            await SendTopic(TopicType);
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle($"Error Running {Name}.")
                                .WithDescription($"Unable to find the {ActionType} command.")
                                .SendEmbed(Context.Channel);
                            break;
                    }
                } else
                    await SendTopic(TopicType);
            } else
                await SendTopic(TopicType);
        }

        public async Task SendTopic(TopicType TopicType) {
            FunTopic FunTopic = await FunTopicsDB.Topics.GetRandomTopic(TopicType);

            string Name = Regex.Replace(TopicType.ToString(), "([A-Z])([a-z]*)", "$1$2");

            if (FunTopic == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"No {Name}s!")
                    .WithDescription($"Heya! I could not find any {Name.ToLower()}s in the database. " +
                        $"To add a {Name.ToLower()} to the database, please use `{BotConfiguration.Prefix}{Name.ToLower()} add BLANK`.")
                    .SendEmbed(Context.Channel);
                return;
            }

            IUser User = DiscordSocketClient.GetUser(FunTopic.ProposerID);

            string Topic = new Regex(@"(^[a-z])|[?!.:;]\s+(.)", RegexOptions.ExplicitCapture)
                .Replace(FunTopic.Topic.ToLower(), String => String.Value.ToUpper());

            await BuildEmbed(EmojiEnum.Sign)
                .WithAuthor(Context.User)
                .WithTitle($"{Context.Client.CurrentUser.Username} Asks")
                .WithDescription(Topic)
                .WithFooter($"{TopicType} Written by {(User == null ? "Unknown" : User.Username)} • " +
                    $"Add a {Name.ToLower()} using `{BotConfiguration.Prefix}{Name.ToLower()} add [TOPIC]`")
                .SendEmbed(Context.Channel);
        }

        public async Task AddTopic(string TopicEntry, TopicType TopicType) {
            TopicEntry = Regex.Replace(TopicEntry, @"[^\u0000-\u007F]+", "");

            string Name = Regex.Replace(TopicType.ToString(), "([A-Z])([a-z]*)", "$1$2");

            if (TopicEntry.Length > 1000) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Add {Name}!")
                    .WithDescription($"Heya! Please cut down on the length of your {Name.ToLower()}. " +
                    $"It should be a maximum of 1000 characters. Currently this character count sits at {TopicEntry.Length}")
                    .SendEmbed(Context.Channel);
                return;
            }

            FunTopic FunTopic = FunTopicsDB.Topics.AsQueryable().Where(Topic => Topic.Topic.Equals(TopicEntry) && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType).FirstOrDefault();

            if (FunTopic != null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Add {Name}!")
                    .WithDescription($"The {Name.ToLower()} `{FunTopic.Topic}` " +
                    $"has already been suggested by {DiscordSocketClient.GetUser(FunTopic.ProposerID).GetUserInformation()}!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await SendForAdminApproval(CreateTopicCallback,
                new Dictionary<string, string>() {
                    { "Topic", TopicEntry },
                    { "TopicType", ( (int) TopicType ).ToString() },
                    { "Proposer", Context.User.Id.ToString() }
                },
                Context.User.Id,
                $"{Context.User.GetUserInformation()} has suggested that the {Name.ToLower()} `{TopicEntry}` should be added to Dexter.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {Name.ToLower()} `{(TopicEntry.Length > 200 ? $"{TopicEntry.Substring(0, 200)}..." : TopicEntry)}` was suggested!")
                .WithDescription($"Once it has passed admin approval, it will be added to the database.")
                .SendEmbed(Context.Channel);
        }

        public void CreateTopicCallback(Dictionary<string, string> Parameters) {
            string Topic = Parameters["Topic"];
            TopicType TopicType = (TopicType)int.Parse(Parameters["TopicType"]);
            ulong Proposer = ulong.Parse(Parameters["Proposer"]);

            FunTopicsDB.Topics.Add(
                new() {
                    Topic = Topic,
                    EntryType = EntryType.Issue,
                    ProposerID = Proposer,
                    TopicID = FunTopicsDB.Topics.Count() + 1,
                    TopicType = TopicType
                }
            );

            FunTopicsDB.SaveChanges();
        }

        public async Task RemoveTopic(int TopicID, TopicType TopicType) {
            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.TopicID == TopicID && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType).FirstOrDefault();

            string Name = Regex.Replace(TopicType.ToString(), "([A-Z])([a-z]*)", "$1$2");

            if (FunTopic == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Remove {Name}.")
                    .WithDescription($"The {Name.ToLower()} {TopicID} does not exist in the database! " +
                    $"Please use the `{BotConfiguration.Prefix}{Name.ToLower()} get " +
                    $"{TopicType.ToString().ToUpper()}` command to get the ID of a {Name.ToLower()}.")
                    .SendEmbed(Context.Channel);
                return;
            }

            await SendForAdminApproval(RemoveTopicCallback,
                new Dictionary<string, string>() {
                    { "TopicID", TopicID.ToString() },
                    { "TopicType", ( (int) TopicType ).ToString() }
                },
                Context.User.Id,
                $"{Context.User.GetUserInformation()} has suggested that the {Name.ToLower()} `{FunTopic.Topic}` should be removed from Dexter.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {Name.ToLower()} `" +
                    $"{(FunTopic.Topic.Length > 200 ? $"{FunTopic.Topic.Substring(0, 200)}..." : FunTopic.Topic)}" +
                    $"` was suggested to be removed!")
                .WithDescription($"Once it has passed admin approval, it will be removed from the database.")
                .SendEmbed(Context.Channel);
        }

        public void RemoveTopicCallback(Dictionary<string, string> Parameters) {
            int TopicID = int.Parse(Parameters["TopicID"]);
            TopicType TopicType = (TopicType)int.Parse(Parameters["TopicType"]);

            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.TopicID == TopicID && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType).FirstOrDefault();

            FunTopic.EntryType = EntryType.Revoke;

            FunTopicsDB.SaveChanges();
        }

        public async Task GetTopic(string TopicEntry, TopicType TopicType) {
            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.Topic.Equals(TopicEntry) && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType).FirstOrDefault();

            string Name = Regex.Replace(TopicType.ToString(), "([A-Z])([a-z]*)", "$1$2");

            if (FunTopic == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Get {Name}.")
                    .WithDescription($"The {Name.ToLower()} `{TopicEntry}` does not exist in the database!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"{TopicType} #{FunTopic.TopicID}")
                .WithDescription(FunTopic.Topic)
                .AddField("Proposer:", DiscordSocketClient.GetUser(FunTopic.ProposerID))
                .AddField("Status:", $"{FunTopic.EntryType}d")
                .SendEmbed(Context.Channel);
        }

        public async Task EditTopic(int TopicID, string EditedTopic, TopicType TopicType) {
            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.TopicID == TopicID && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType).FirstOrDefault();

            string Name = Regex.Replace(TopicType.ToString(), "([A-Z])([a-z]*)", "$1$2");

            if (FunTopic == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Edit {Name}.")
                    .WithDescription($"The {Name.ToLower()} {TopicID} does not exist in the database! " +
                        $"Please use the `{BotConfiguration.Prefix}{Name.ToLower()} get " +
                        $"{TopicType.ToString().ToUpper()}` command to get the ID of a {Name.ToLower()}.")
                    .SendEmbed(Context.Channel);
            }

            await SendForAdminApproval(EditTopicCallback,
                new Dictionary<string, string>() {
                    { "TopicID", TopicID.ToString() },
                    { "TopicType", ( (int) TopicType ).ToString() },
                    { "EditedTopic", EditedTopic }
                },
                Context.User.Id,
                $"{Context.User.GetUserInformation()} has suggested that the {Name.ToLower()} `{FunTopic.Topic}` should be changed to `{EditedTopic}`."
            );

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {Name.ToLower()} `" +
                $"{(FunTopic.Topic.Length > 100 ? $"{FunTopic.Topic.Substring(0, 100)}..." : FunTopic.Topic)}" +
                $"` was suggested to be edited to `" +
                $"{(EditedTopic.Length > 100 ? $"{EditedTopic.Substring(0, 100)}..." : EditedTopic)}" +
                $"`!")
                .WithDescription($"Once it has passed admin approval, it will be edited in the database accordingly.")
                .SendEmbed(Context.Channel);
        }

        public void EditTopicCallback(Dictionary<string, string> Parameters) {
            int TopicID = int.Parse(Parameters["TopicID"]);
            TopicType TopicType = (TopicType)int.Parse(Parameters["TopicType"]);
            string EditedTopic = Parameters["EditedTopic"];

            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.TopicID == TopicID && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType).FirstOrDefault();

            FunTopic.Topic = EditedTopic;

            FunTopicsDB.SaveChanges();
        }

    }

}
