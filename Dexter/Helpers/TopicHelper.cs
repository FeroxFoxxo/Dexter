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

        /// <summary>
        /// Manages the modular tasks pertaining to modification of the topic database(s).
        /// Or alternatively runs the topic command as usual if no reasonable alias or syntax can be leveraged.
        /// </summary>
        /// <param name="Command">The entire list of arguments used for the command, stringified.</param>
        /// <param name="TopicType">What type of topic database should be accessed. Either 'topic' or 'wyr'.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task RunTopic (string Command, TopicType TopicType) {
            string Name = Regex.Replace(TopicType.ToString(), "([A-Z])([a-z]*)", " $1$2").Substring(1);

            if (!string.IsNullOrEmpty(Command)) {
                if (Enum.TryParse(Command.Split(" ")[0].ToLower().Pascalize(), out Enums.ActionType ActionType)) {
                    if(RestrictionsDB.IsUserRestricted(Context.User, Databases.UserRestrictions.Restriction.TopicManagement) && ActionType != Enums.ActionType.Get) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You aren't permitted to manage topics!")
                            .WithDescription("You have been blacklisted from using this service. If you think this is a mistake, feel free to personally contact an administrator")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    string Topic = Command[(Command.Split(" ")[0].Length + 1)..];

                    switch (ActionType) {
                        case Enums.ActionType.Add:
                            await AddTopic(Topic, TopicType, Name);
                            break;
                        case Enums.ActionType.Get:
                            await GetTopic(Topic, TopicType, Name);
                            break;
                        case Enums.ActionType.Remove:
                            if (int.TryParse(Topic, out int TopicID))
                                await RemoveTopic(TopicID, TopicType, Name);
                            else
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle($"Error Removing {Name}.")
                                    .WithDescription($"No {Name.ToLower()} ID provided! To use this command please use the syntax of `remove [ID]`.")
                                    .SendEmbed(Context.Channel);
                            break;
                        case Enums.ActionType.Edit:
                            if (int.TryParse(Topic.Split(' ')[0], out int EditTopicID))
                                await EditTopic(EditTopicID, string.Join(' ', Topic.Split(' ').Skip(1).ToArray()), TopicType, Name);
                            else
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle($"Error Editing {Name}.")
                                    .WithDescription($"No {Name.ToLower()} ID provided! To use this command please use the syntax of `edit [ID] [{Name.ToUpper()}]`.")
                                    .SendEmbed(Context.Channel);
                            break;
                        case Enums.ActionType.Unknown:
                            await SendTopic(TopicType, Name);
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle($"Error Running {Name}.")
                                .WithDescription($"Unable to find the {ActionType} command.")
                                .SendEmbed(Context.Channel);
                            break;
                    }
                } else
                    await SendTopic(TopicType, Name);
            } else
                await SendTopic(TopicType, Name);
        }

        /// <summary>
        /// Sends a randomly selected topic or wyr to chat.
        /// </summary>
        /// <param name="TopicType">Which type of topic database to access.</param>
        /// <param name="Name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task SendTopic(TopicType TopicType, string Name) {
            FunTopic FunTopic = await FunTopicsDB.Topics.GetRandomTopic(TopicType);

            if (FunTopic == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"No {Name}s!")
                    .WithDescription($"Heya! I could not find any {Name.ToLower()}s in the database. " +
                        $"To add a {Name.ToLower()} to the database, please use `{BotConfiguration.Prefix}{TopicType.ToString().ToLower()} add [{Name.ToUpper()}]`.")
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
                .WithFooter($"{Name} Written by {(User == null ? "Unknown" : User.Username)} • " +
                    $"Add a {Name.ToLower()} using {BotConfiguration.Prefix}{TopicType.ToString().ToLower()} add {Name.ToUpper()}")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Launches a request to set up a new topic in the corresponding topic type database.
        /// </summary>
        /// <remarks>This process requires intermediary administrator approval before completion.</remarks>
        /// <param name="TopicEntry">The string pertaining to the whole expression of the new suggested topic.</param>
        /// <param name="TopicType">Which type of topic database to access.</param>
        /// <param name="Name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task AddTopic(string TopicEntry, TopicType TopicType, string Name) {
            TopicEntry = Regex.Replace(TopicEntry, @"[^\u0000-\u007F]+", "");

            if (TopicEntry.Length > 1000) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Add {Name}!")
                    .WithDescription($"Heya! Please cut down on the length of your {Name.ToLower()}. " +
                    $"It should be a maximum of 1000 characters. Currently this character count sits at {TopicEntry.Length}")
                    .SendEmbed(Context.Channel);
                return;
            }

            FunTopic FunTopic = FunTopicsDB.Topics
                .AsQueryable()
                .Where(Topic => Topic.Topic.Equals(TopicEntry) && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType)
                .FirstOrDefault();

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

        /// <summary>
        /// Directly adds a new topic to the corresponding database.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "Topic", "TopicType" and "Proposer".
        /// These should be parsable to a <c>string</c>, <c>TopicType</c>, and <c>ulong</c> (IUser ID) respectively.
        /// </param>

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

        /// <summary>
        /// Launches a request to remove an existing topic from the topic database by ID.
        /// </summary>
        /// <remarks>This process requires an intermediary administrator approval phase.</remarks>
        /// <param name="TopicID">The numerical ID corresponding to the target topic.</param>
        /// <param name="TopicType">Which type of topic database to access.</param>
        /// <param name="Name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task RemoveTopic(int TopicID, TopicType TopicType, string Name) {
            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(Topic => Topic.TopicID == TopicID && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType)
                    .FirstOrDefault();

            if (FunTopic == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Remove {Name}.")
                    .WithDescription($"The {Name.ToLower()} {TopicID} does not exist in the database! " +
                    $"Please use the `{BotConfiguration.Prefix}{TopicType.ToString().ToLower()} get " +
                    $"[{Name.ToUpper()}]` command to get the ID of a {Name.ToLower()}.")
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

        /// <summary>
        /// Directly removes a topic from the corresponding database.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "TopicID" and "TopicType".
        /// These should be parsable to a <c>int</c> and <c>TopicType</c> respectively.
        /// </param>

        public void RemoveTopicCallback(Dictionary<string, string> Parameters) {
            int TopicID = int.Parse(Parameters["TopicID"]);
            TopicType TopicType = (TopicType)int.Parse(Parameters["TopicType"]);

            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(Topic => Topic.TopicID == TopicID && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType)
                    .FirstOrDefault();

            FunTopic.EntryType = EntryType.Revoke;

            FunTopicsDB.SaveChanges();
        }

        /// <summary>
        /// Sends in an embed detailing a specific topic and its related data.
        /// </summary>
        /// <param name="TopicEntry">The exact text corresponding to the topic.</param>
        /// <param name="TopicType">Which type of topic database to access.</param>
        /// <param name="Name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task GetTopic(string TopicEntry, TopicType TopicType, string Name) {
            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(Topic => Topic.Topic.Equals(TopicEntry) && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType)
                    .FirstOrDefault();

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

        /// <summary>
        /// Sends in a request to edit the comment attached to a specified topic by ID.
        /// </summary>
        /// <remarks>This process requires intermediary admin approval before enaction.</remarks>
        /// <param name="TopicID">The numerical ID of the target topic.</param>
        /// <param name="EditedTopic">The new text expression for the target topic.</param>
        /// <param name = "TopicType" > Which type of topic database to access.</param>
        /// <param name="Name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task EditTopic(int TopicID, string EditedTopic, TopicType TopicType, string Name) {
            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(Topic => Topic.TopicID == TopicID && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType)
                    .FirstOrDefault();

            if (FunTopic == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Edit {Name}.")
                    .WithDescription($"The {Name.ToLower()} {TopicID} does not exist in the database! " +
                        $"Please use the `{BotConfiguration.Prefix}{Name.ToLower()} get " +
                        $"[{Name.ToUpper()}]` command to get the ID of a {Name.ToLower()}.")
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

        /// <summary>
        /// Directly edits an already-existing topic in the corresponding database.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a definition for "TopicID", "TopicType", and "EditedTopic".
        /// These should be parsable to an <c>int</c>, <c>TopicType</c>, and <c>string</c> respectively.
        /// </param>

        public void EditTopicCallback(Dictionary<string, string> Parameters) {
            int TopicID = int.Parse(Parameters["TopicID"]);
            TopicType TopicType = (TopicType)int.Parse(Parameters["TopicType"]);
            string EditedTopic = Parameters["EditedTopic"];

            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(Topic => Topic.TopicID == TopicID && Topic.EntryType == EntryType.Issue && Topic.TopicType == TopicType)
                    .FirstOrDefault();

            FunTopic.Topic = EditedTopic;

            FunTopicsDB.SaveChanges();
        }

    }

}
