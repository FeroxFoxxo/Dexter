using Dexter.Abstractions;
using Dexter.Databases.FunTopics;
using Dexter.Databases.UserRestrictions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands
{

    /// <summary>
    /// The class containing all commands within the Fun module.
    /// </summary>

    public partial class TopicCommands : DiscordModule
    {

        /// <summary>
        /// Loads the database containing topics for the <c>~topic</c> command.
        /// </summary>

        public FunTopicsDB FunTopicsDB { get; set; }

        /// <summary>
        /// Holds relevant information about permissions and restrictions for specific users and services.
        /// </summary>

        public RestrictionsDB RestrictionsDB { get; set; }

        /// <summary>
        /// Manages the modular tasks pertaining to modification of the topic database(s).
        /// Or alternatively runs the topic command as usual if no reasonable alias or syntax can be leveraged.
        /// </summary>
        /// <param name="command">The entire list of arguments used for the command, stringified.</param>
        /// <param name="topicType">What type of topic database should be accessed. Either 'topic' or 'wyr'.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task RunTopic(string command, TopicType topicType)
        {
            string name = Regex.Replace(topicType.ToString(), "([A-Z])([a-z]*)", " $1$2")[1..];

            if (!string.IsNullOrEmpty(command))
            {
                if (Enum.TryParse(command.Split(" ")[0].ToLower().Pascalize(), out Enums.ActionType actionType))
                {
                    if (RestrictionsDB.IsUserRestricted(Context.User, Databases.UserRestrictions.Restriction.TopicManagement) && actionType != Enums.ActionType.Get)
                    {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You aren't permitted to manage topics!")
                            .WithDescription("You have been blacklisted from using this service. If you think this is a mistake, feel free to personally contact an administrator")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    string topic = command[(command.Split(" ")[0].Length + 1)..];

                    switch (actionType)
                    {
                        case Enums.ActionType.Add:
                            await AddTopic(topic, topicType, name);
                            break;
                        case Enums.ActionType.Get:
                            await GetTopic(topic, topicType, name);
                            break;
                        case Enums.ActionType.Remove:
                            if (int.TryParse(topic, out int topicID))
                                await RemoveTopic(topicID, topicType, name);
                            else
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle($"Error Removing {name}.")
                                    .WithDescription($"No {name.ToLower()} ID provided! To use this command please use the syntax of `remove ID`.")
                                    .SendEmbed(Context.Channel);
                            break;
                        case Enums.ActionType.Edit:
                            if (int.TryParse(topic.Split(' ')[0], out int editTopicID))
                                await EditTopic(editTopicID, string.Join(' ', topic.Split(' ').Skip(1).ToArray()), topicType, name);
                            else
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle($"Error Editing {name}.")
                                    .WithDescription($"No {name.ToLower()} ID provided! To use this command please use the syntax of `edit ID {name.ToUpper()}`.")
                                    .SendEmbed(Context.Channel);
                            break;
                        case Enums.ActionType.Unknown:
                            await SendTopic(topicType, name);
                            break;
                        default:
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle($"Error Running {name}.")
                                .WithDescription($"Unable to find the {actionType} command.")
                                .SendEmbed(Context.Channel);
                            break;
                    }
                }
                else
                    await SendTopic(topicType, name);
            }
            else
                await SendTopic(topicType, name);
        }

        /// <summary>
        /// Sends a randomly selected topic or wyr to chat.
        /// </summary>
        /// <param name="topicType">Which type of topic database to access.</param>
        /// <param name="name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task SendTopic(TopicType topicType, string name)
        {
            FunTopic funTopic = GetRandomTopic(topicType);

            if (funTopic == null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"No {name}s!")
                    .WithDescription($"Heya! I could not find any {name.ToLower()}s in the database. " +
                        $"To add a {name.ToLower()} to the database, please use `{BotConfiguration.Prefix}{topicType.ToString().ToLower()} add {name.ToUpper()}`.")
                    .SendEmbed(Context.Channel);
                return;
            }

            IUser user = DiscordShardedClient.GetUser(funTopic.ProposerID);

            string topic = new Regex(@"(^[a-z])|[?!.:;]\s+(.)", RegexOptions.ExplicitCapture)
                .Replace(funTopic.Topic.ToLower(), String => String.Value.ToUpper());

            await BuildEmbed(EmojiEnum.Sign)
                .WithAuthor(Context.User)
                .WithTitle($"{Context.Client.CurrentUser.Username} Asks")
                .WithDescription(topic)
                .WithFooter($"{name} written by {(user == null ? "Unknown" : user.Username)} • " +
                    $"Add a {name.ToLower()} using {BotConfiguration.Prefix}{topicType.ToString().ToLower()} add {name.ToUpper()}")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Launches a request to set up a new topic in the corresponding topic type database.
        /// </summary>
        /// <remarks>This process requires intermediary administrator approval before completion.</remarks>
        /// <param name="topicEntry">The string pertaining to the whole expression of the new suggested topic.</param>
        /// <param name="topicType">Which type of topic database to access.</param>
        /// <param name="name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task AddTopic(string topicEntry, TopicType topicType, string name)
        {
            topicEntry = Regex.Replace(topicEntry, @"[^\u0000-\u007F]+", "");

            if (topicEntry.Length > 1000)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Add {name}!")
                    .WithDescription($"Heya! Please cut down on the length of your {name.ToLower()}. " +
                    $"It should be a maximum of 1000 characters. Currently this character count sits at {topicEntry.Length}")
                    .SendEmbed(Context.Channel);
                return;
            }

            FunTopic funTopic = FunTopicsDB.Topics
                .AsQueryable()
                .Where(topic => topic.Topic.Equals(topicEntry) && topic.EntryType == EntryType.Issue && topic.TopicType == topicType)
                .FirstOrDefault();

            if (funTopic != null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Add {name}!")
                    .WithDescription($"The {name.ToLower()} `{funTopic.Topic}` " +
                    $"has already been suggested by {DiscordShardedClient.GetUser(funTopic.ProposerID).GetUserInformation()}!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await SendForAdminApproval(CreateTopicCallback,
                new Dictionary<string, string>() {
                    { "Topic", topicEntry },
                    { "TopicType", ( (int) topicType ).ToString() },
                    { "Proposer", Context.User.Id.ToString() }
                },
                Context.User.Id,
                $"{Context.User.GetUserInformation()} has suggested that the {name.ToLower()} `{topicEntry}` should be added to Dexter.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {name.ToLower()} `{(topicEntry.Length > 200 ? $"{topicEntry.Substring(0, 200)}..." : topicEntry)}` was suggested!")
                .WithDescription($"Once it has passed admin approval, it will be added to the database.")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Directly adds a new topic to the corresponding database.
        /// </summary>
        /// <param name="parameters">
        /// A string-string dictionary containing a definition for "Topic", "TopicType" and "Proposer".
        /// These should be parsable to a <c>string</c>, <c>TopicType</c>, and <c>ulong</c> (IUser ID) respectively.
        /// </param>

        public void CreateTopicCallback(Dictionary<string, string> parameters)
        {
            string topic = parameters["Topic"];
            TopicType topicType = (TopicType)int.Parse(parameters["TopicType"]);
            ulong proposer = ulong.Parse(parameters["Proposer"]);

            FunTopicsDB.Topics.Add(
                new()
                {
                    Topic = topic,
                    EntryType = EntryType.Issue,
                    ProposerID = proposer,
                    TopicID = FunTopicsDB.Topics.Count() + 1,
                    TopicType = topicType
                }
            );

            FunTopicsDB.SaveChanges();
        }

        /// <summary>
        /// Launches a request to remove an existing topic from the topic database by ID.
        /// </summary>
        /// <remarks>This process requires an intermediary administrator approval phase.</remarks>
        /// <param name="topicID">The numerical ID corresponding to the target topic.</param>
        /// <param name="topicType">Which type of topic database to access.</param>
        /// <param name="name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task RemoveTopic(int topicID, TopicType topicType, string name)
        {
            FunTopic funTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(topic => topic.TopicID == topicID && topic.EntryType == EntryType.Issue && topic.TopicType == topicType)
                    .FirstOrDefault();

            if (funTopic == null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Remove {name}.")
                    .WithDescription($"The {name.ToLower()} {topicID} does not exist in the database! " +
                    $"Please use the `{BotConfiguration.Prefix}{topicType.ToString().ToLower()} get " +
                    $"{name.ToUpper()}` command to get the ID of a {name.ToLower()}.")
                    .SendEmbed(Context.Channel);
                return;
            }

            await SendForAdminApproval(RemoveTopicCallback,
                new Dictionary<string, string>() {
                    { "TopicID", topicID.ToString() },
                    { "TopicType", ( (int) topicType ).ToString() }
                },
                Context.User.Id,
                $"{Context.User.GetUserInformation()} has suggested that the {name.ToLower()} `{funTopic.Topic}` should be removed from Dexter.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {name.ToLower()} `" +
                    $"{(funTopic.Topic.Length > 200 ? $"{funTopic.Topic.Substring(0, 200)}..." : funTopic.Topic)}" +
                    $"` was suggested to be removed!")
                .WithDescription($"Once it has passed admin approval, it will be removed from the database.")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Directly removes a topic from the corresponding database.
        /// </summary>
        /// <param name="parameters">
        /// A string-string dictionary containing a definition for "TopicID" and "TopicType".
        /// These should be parsable to a <c>int</c> and <c>TopicType</c> respectively.
        /// </param>

        public void RemoveTopicCallback(Dictionary<string, string> parameters)
        {
            int topicID = int.Parse(parameters["TopicID"]);
            TopicType topicType = (TopicType)int.Parse(parameters["TopicType"]);

            FunTopic funTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(topic => topic.TopicID == topicID && topic.EntryType == EntryType.Issue && topic.TopicType == topicType)
                    .FirstOrDefault();

            funTopic.EntryType = EntryType.Revoke;

            FunTopicsDB.SaveChanges();
        }

        /// <summary>
        /// Sends in an embed detailing a specific topic and its related data.
        /// </summary>
        /// <param name="topicEntry">The exact text corresponding to the topic.</param>
        /// <param name="topicType">Which type of topic database to access.</param>
        /// <param name="name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task GetTopic(string topicEntry, TopicType topicType, string name)
        {
            FunTopic funTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(topic => topic.Topic.Equals(topicEntry) && topic.EntryType == EntryType.Issue && topic.TopicType == topicType)
                    .FirstOrDefault();

            if (funTopic == null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Get {name}.")
                    .WithDescription($"The {name.ToLower()} `{topicEntry}` does not exist in the database!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"{topicType} #{funTopic.TopicID}")
                .WithDescription(funTopic.Topic)
                .AddField("Proposer:", DiscordShardedClient.GetUser(funTopic.ProposerID))
                .AddField("Status:", $"{funTopic.EntryType}d")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Sends in a request to edit the comment attached to a specified topic by ID.
        /// </summary>
        /// <remarks>This process requires intermediary admin approval before enaction.</remarks>
        /// <param name="topicID">The numerical ID of the target topic.</param>
        /// <param name="editedTopic">The new text expression for the target topic.</param>
        /// <param name = "topicType" > Which type of topic database to access.</param>
        /// <param name="name">The name of the type of topic, generated using regex.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task EditTopic(int topicID, string editedTopic, TopicType topicType, string name)
        {
            FunTopic FunTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(topic => topic.TopicID == topicID && topic.EntryType == EntryType.Issue && topic.TopicType == topicType)
                    .FirstOrDefault();

            if (FunTopic == null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Unable To Edit {name}.")
                    .WithDescription($"The {name.ToLower()} {topicID} does not exist in the database! " +
                        $"Please use the `{BotConfiguration.Prefix}{name.ToLower()} get " +
                        $"{name.ToUpper()}` command to get the ID of a {name.ToLower()}.")
                    .SendEmbed(Context.Channel);
            }

            await SendForAdminApproval(EditTopicCallback,
                new Dictionary<string, string>() {
                    { "TopicID", topicID.ToString() },
                    { "TopicType", ( (int) topicType ).ToString() },
                    { "EditedTopic", editedTopic }
                },
                Context.User.Id,
                $"{Context.User.GetUserInformation()} has suggested that the {name.ToLower()} `{FunTopic.Topic}` should be changed to `{editedTopic}`."
            );

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {name.ToLower()} `" +
                $"{(FunTopic.Topic.Length > 100 ? $"{FunTopic.Topic.Substring(0, 100)}..." : FunTopic.Topic)}" +
                $"` was suggested to be edited to `" +
                $"{(editedTopic.Length > 100 ? $"{editedTopic.Substring(0, 100)}..." : editedTopic)}" +
                $"`!")
                .WithDescription($"Once it has passed admin approval, it will be edited in the database accordingly.")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Directly edits an already-existing topic in the corresponding database.
        /// </summary>
        /// <param name="parameters">
        /// A string-string dictionary containing a definition for "TopicID", "TopicType", and "EditedTopic".
        /// These should be parsable to an <c>int</c>, <c>TopicType</c>, and <c>string</c> respectively.
        /// </param>

        public void EditTopicCallback(Dictionary<string, string> parameters)
        {
            int topicID = int.Parse(parameters["TopicID"]);
            TopicType topicType = (TopicType)int.Parse(parameters["TopicType"]);
            string editedTopic = parameters["EditedTopic"];

            FunTopic funTopic = FunTopicsDB.Topics
                    .AsQueryable()
                    .Where(topic => topic.TopicID == topicID && topic.EntryType == EntryType.Issue && topic.TopicType == topicType)
                    .FirstOrDefault();

            funTopic.Topic = editedTopic;

            FunTopicsDB.SaveChanges();
        }

        /// <summary>
        /// The GetRandomTopic command extends upon a database set and returns a random, valid entry.
        /// </summary>
        /// <param name="topicType">The type of topic to draw from. It may be a TOPIC or a WOULDYOURATHER.</param>
        /// <returns>A tasked result of an instance of a fun object.</returns>

        public FunTopic GetRandomTopic(TopicType topicType)
        {
            FunTopic[] eligible = FunTopicsDB.Topics.AsQueryable().Where(t => t.TopicType == topicType && t.EntryType == EntryType.Issue).ToArray();

            if (!eligible.Any())
                return null;

            int randomID = new Random().Next(0, eligible.Length);

            return eligible[randomID];
        }

    }

}
