using Dexter.Databases.FunTopics;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class FunCommands {

        public async Task SendTopic(TopicType TopicType) {
            FunTopic FunTopic = TopicType switch {
                TopicType.Topic => await FunTopicsDB.Topics.GetRandomTopic(),
                TopicType.WouldYouRather => await FunTopicsDB.WouldYouRather.GetRandomTopic(),
                _ => null,
            };

            if (FunTopic == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"No {TopicType}s!")
                    .WithDescription($"Heya! I could not find any {TopicType.ToString().ToLower()}s in the database. " +
                        $"To add a {TopicType.ToString().ToLower()} to the database, please use `{BotConfiguration.Prefix}{TopicType.ToString().ToLower()} add BLANK`.")
                    .SendEmbed(Context.Channel);
                return;
            }

            await BuildEmbed(EmojiEnum.Sign)
                .WithAuthor(Context.Message.Author)
                .WithTitle($"{Context.Client.CurrentUser.Username} Asks")
                .WithDescription(FunTopic.Topic)
                .WithFooter($"{TopicType} Written by {DiscordSocketClient.GetUser(FunTopic.ProposerID).Username}.")
                .SendEmbed(Context.Channel);
        }

        public async Task AddTopic(string TopicEntry, TopicType TopicType) {
            TopicEntry = Regex.Replace(TopicEntry, @"[^\u0000-\u007F]+", "");

            if (TopicEntry.Length > 1000)
                throw new InvalidOperationException($"Heya! Please cut down on the length of your {TopicType.ToString().ToLower()}. " +
                    $"It should be a maximum of 1000 characters. Currently this character count sits at {TopicEntry.Length}");

            FunTopic FunTopic = TopicType switch {
                TopicType.Topic => FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.Topic.Equals(TopicEntry)).FirstOrDefault(),
                
                TopicType.WouldYouRather => FunTopicsDB.WouldYouRather
                    .AsQueryable().Where(Topic => Topic.Topic.Equals(TopicEntry)).FirstOrDefault(),
                
                _ => null,
            };

            if (FunTopic != null)
                throw new InvalidOperationException($"The {TopicType.ToString().ToLower()} `{FunTopic.Topic}` " +
                    $"has already been suggested by {DiscordSocketClient.GetUser(FunTopic.ProposerID).GetUserInformation()}!");

            await SendForAdminApproval(CreateTopicCallback,
                new Dictionary<string, string>() {
                    { "Topic", TopicEntry },
                    { "TopicType", ( (int) TopicType ).ToString() },
                    { "Proposer", Context.Message.Author.Id.ToString() }
                },
                Context.Message.Author.Id,
                $"{Context.Message.Author.GetUserInformation()} has suggested that the {TopicType.ToString().ToLower()} `{TopicEntry}` should be added to Dexter.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {TopicType.ToString().ToLower()} `{TopicEntry}` was suggested!")
                .WithDescription($"Once it has passed admin approval, it will be added to the database.")
                .SendEmbed(Context.Channel);
        }

        public async Task CreateTopicCallback(Dictionary<string, string> Parameters) {
            string Topic = Parameters["Topic"];
            TopicType TopicType = (TopicType)int.Parse(Parameters["TopicType"]);
            ulong Proposer = ulong.Parse(Parameters["Proposer"]);

            DbSet<FunTopic> TopicDatabase = TopicType switch {
                TopicType.Topic => FunTopicsDB.Topics,
                TopicType.WouldYouRather => FunTopicsDB.WouldYouRather,
                _ => null
            };

            FunTopic FunTopic = new () {
                Topic = Topic,
                EntryType = EntryType.Issue,
                ProposerID = Proposer,
                TopicID = TopicDatabase.Count() + 1
            };

            TopicDatabase.Add(FunTopic);

            await FunTopicsDB.SaveChangesAsync();
        }

        public async Task RemoveTopic(int TopicID, TopicType TopicType) {
            FunTopic FunTopic = TopicType switch {
                TopicType.Topic => FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.TopicID.Equals(TopicID)).FirstOrDefault(),

                TopicType.WouldYouRather => FunTopicsDB.WouldYouRather
                    .AsQueryable().Where(Topic => Topic.TopicID.Equals(TopicID)).FirstOrDefault(),

                _ => null,
            };

            if (FunTopic == null)
                throw new InvalidOperationException($"The {TopicType.ToString().ToLower()} {TopicID} does not exist in the database! " +
                    $"Please use the `{BotConfiguration.Prefix}{TopicType.ToString().ToLower()} get " +
                    $"{TopicType.ToString().ToUpper()}` command to get the ID of a {TopicType.ToString().ToLower()}.");

            await SendForAdminApproval(RemoveTopicCallback,
                new Dictionary<string, string>() {
                    { "TopicID", TopicID.ToString() },
                    { "TopicType", ( (int) TopicType ).ToString() }
                },
                Context.Message.Author.Id,
                $"{Context.Message.Author.GetUserInformation()} has suggested that the {TopicType.ToString().ToLower()} `{FunTopic.Topic}` should be removed from Dexter.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {TopicType.ToString().ToLower()} `{FunTopic.Topic}` was suggested to be removed!")
                .WithDescription($"Once it has passed admin approval, it will be removed from the database.")
                .SendEmbed(Context.Channel);
        }

        public async Task RemoveTopicCallback(Dictionary<string, string> Parameters) {
            int TopicID = int.Parse(Parameters["TopicID"]);
            TopicType TopicType = (TopicType)int.Parse(Parameters["TopicType"]);

            switch (TopicType) {
                case TopicType.Topic:
                    FunTopicsDB.Topics.Remove(FunTopicsDB.Topics.AsQueryable().Where(Topic => Topic.TopicID == TopicID).FirstOrDefault());
                    break;
                case TopicType.WouldYouRather:
                    FunTopicsDB.Topics.Remove(FunTopicsDB.WouldYouRather.AsQueryable().Where(Topic => Topic.TopicID == TopicID).FirstOrDefault());
                    break;
            };

            await FunTopicsDB.SaveChangesAsync();
        }

        public async Task GetTopic(string TopicEntry, TopicType TopicType) {
            FunTopic FunTopic = TopicType switch {
                TopicType.Topic => FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.Topic.Equals(TopicEntry)).FirstOrDefault(),

                TopicType.WouldYouRather => FunTopicsDB.WouldYouRather
                    .AsQueryable().Where(Topic => Topic.Topic.Equals(TopicEntry)).FirstOrDefault(),

                _ => null,
            };

            if (FunTopic == null)
                throw new InvalidOperationException($"The {TopicType.ToString().ToLower()} `{TopicEntry}` does not exist in the database!");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"{TopicType} #{FunTopic.TopicID}")
                .WithDescription(FunTopic.Topic)
                .AddField("Proposer:", DiscordSocketClient.GetUser(FunTopic.ProposerID))
                .AddField("Status:", FunTopic.EntryType)
                .SendEmbed(Context.Channel);
        }

        public async Task EditTopic(int TopicID, string EditedTopic, TopicType TopicType) {
            FunTopic FunTopic = TopicType switch {
                TopicType.Topic => FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.TopicID.Equals(TopicID)).FirstOrDefault(),

                TopicType.WouldYouRather => FunTopicsDB.WouldYouRather
                    .AsQueryable().Where(Topic => Topic.TopicID.Equals(TopicID)).FirstOrDefault(),

                _ => null,
            };

            if (FunTopic == null)
                throw new InvalidOperationException($"The {TopicType.ToString().ToLower()} {TopicID} does not exist in the database! " +
                    $"Please use the `{BotConfiguration.Prefix}{TopicType.ToString().ToLower()} get " +
                    $"{TopicType.ToString().ToUpper()}` command to get the ID of a {TopicType.ToString().ToLower()}.");

            await SendForAdminApproval(EditTopicCallback,
                new Dictionary<string, string>() {
                    { "TopicID", TopicID.ToString() },
                    { "TopicType", ( (int) TopicType ).ToString() },
                    { "EditedTopic", EditedTopic }
                },
                Context.Message.Author.Id,
                $"{Context.Message.Author.GetUserInformation()} has suggested that the {TopicType.ToString().ToLower()} `{FunTopic.Topic}` should be changed to {EditedTopic}."
            );

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The {TopicType.ToString().ToLower()} `{FunTopic.Topic}` was suggested to be edited to {EditedTopic}!")
                .WithDescription($"Once it has passed admin approval, it will be edited in the database accordingly.")
                .SendEmbed(Context.Channel);
        }

        public async Task EditTopicCallback(Dictionary<string, string> Parameters) {
            int TopicID = int.Parse(Parameters["TopicID"]);
            TopicType TopicType = (TopicType)int.Parse(Parameters["TopicType"]);
            string EditedTopic = Parameters["EditedTopic"];

            FunTopic FunTopic = TopicType switch {
                TopicType.Topic => FunTopicsDB.Topics
                    .AsQueryable().Where(Topic => Topic.TopicID.Equals(TopicID)).FirstOrDefault(),

                TopicType.WouldYouRather => FunTopicsDB.WouldYouRather
                    .AsQueryable().Where(Topic => Topic.TopicID.Equals(TopicID)).FirstOrDefault(),

                _ => null,
            };

            FunTopic.Topic = EditedTopic;

            await FunTopicsDB.SaveChangesAsync();
        }
    }

}
