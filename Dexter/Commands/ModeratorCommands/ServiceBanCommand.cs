using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.UserRestrictions;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;

namespace Dexter.Commands {
    partial class ModeratorCommands {

        /// <summary>
        /// Manages the addition, removal, and fetching of Restrictions for specific <paramref name="User"/>s.
        /// </summary>
        /// <param name="Action">What action to take on the <paramref name="User"/>'s restrictions.</param>
        /// <param name="User">The user to target with <paramref name="Action"/>.</param>
        /// <param name="Restrictions">The Restrictions to target with <paramref name="Action"/> in <paramref name="User"/>. Optional when using 'get'.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("serviceban")]
        [Alias("serviceblacklist", "commandblacklist", "cmdblacklist", "cmdban")]
        [Summary("Manages the removal of permissions from specific users for a variety of bot features.\n" +
            "`ADD [User] [Restriction] (...)` - Add one or more restrictions to a target user.\n" +
            "`REMOVE [User] [Restriction] (...)` - Remove one or more restrictions from a user.\n" +
            "`GET [User]` - Gets all restrictions affecting a user.\n" +
            "*Note:* For all valid Restrictions check out `~listservicebans`.")]
        [RequireModerator]
        [BotChannel]

        public async Task ServiceBanCommand(string Action, IUser User, [Remainder] string Restrictions = "") {
            string[] RestrictionsArray = Restrictions.Trim().Split(" ");

            Restriction Apply;
            bool[] Success;
            List<string> Errored = new();
            
            switch(Action.ToLower()) {
                case "add":
                    Apply = ParseRestriction(RestrictionsArray, out Success);

                    for(int i = 0; i < Success.Length; i++) {
                        if (!Success[i]) Errored.Add(RestrictionsArray[i]);
                    }

                    if(!RestrictionsDB.AddRestriction(User, Apply)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Failure!")
                            .WithDescription($"User {User.GetUserInformation()} already has the following restrictions applied: \n" +
                            $"{Apply}")
                            .AddField(Errored.Count > 0, "Unrecognizable:", string.Join(", ", Errored))
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    RestrictionsDB.SaveChanges();

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Success!")
                        .WithDescription($"User {User.GetUserInformation()} has had the following permissions revoked: \n" +
                            $"{Apply}.")
                        .AddField(Errored.Count > 0, "Unrecognizable:", string.Join(", ", Errored))
                        .SendEmbed(Context.Channel);
                    return;
                case "remove":
                    Apply = ParseRestriction(RestrictionsArray, out Success);

                    for (int i = 0; i < Success.Length; i++) {
                        if (!Success[i]) Errored.Add(RestrictionsArray[i]);
                    }

                    if (!RestrictionsDB.RemoveRestriction(User, Apply)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Failure!")
                            .WithDescription($"User {User.GetUserInformation()} has none of the following restrictions: \n" +
                            $"{Apply}")
                            .AddField(Errored.Count > 0, "Unrecognizable:", string.Join(", ", Errored))
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    RestrictionsDB.SaveChanges();

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Success!")
                        .WithDescription($"User {User.GetUserInformation()} has had the following restrictions removed: \n" +
                            $"{Apply}.")
                        .AddField(Errored.Count > 0, "Unrecognizable:", string.Join(", ", Errored))
                        .SendEmbed(Context.Channel);
                    return;
                case "get":
                case "fetch":
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"{User.Username.Possessive()} Restrictions:")
                        .WithDescription(RestrictionsDB.GetUserRestrictions(User).ToString())
                        .SendEmbed(Context.Channel);
                    return;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to parse action!")
                        .WithDescription($"I wasn't able to parse Action `{Action}`, please use `ADD`, `REMOVE`, or `GET`")
                        .SendEmbed(Context.Channel);
                    return;
            }
        }

        private Restriction ParseRestriction(string[] Input, out bool[] Success) {
            Restriction Result = Restriction.None;
            Success = new bool[Input.Length];

            for(int i = 0; i < Input.Length; i++) {
                string R = Input[i];
                if (Enum.TryParse(R, true, out Restriction NewR)) {
                    Result |= NewR;
                    Success[i] = true;
                } else {
                    switch (R.ToLower()) {
                        case "suggest":
                        case "suggestion":
                            Result |= Restriction.Suggestions;
                            Success[i] = true;
                            break;
                        case "topic":
                        case "topics":
                        case "wyr":
                            Result |= Restriction.TopicManagement;
                            Success[i] = true;
                            break;
                        case "all":
                            foreach(ulong r in Enum.GetValues<Restriction>()) {
                                Result |= (Restriction) r;
                            }
                            Success[i] = true;
                            break;
                    }
                }
            }

            return Result;
        }

        /// <summary>
        /// Sends a list of valid bannable services.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("listservicebans")]
        [Summary("Lists all valid Restriction names (not case-sensitive).")]
        [RequireModerator]
        [BotChannel]

        public async Task ListBannableServicesCommand() {
            await Context.Message.ReplyAsync($"Valid expressions are: ***{string.Join("***, ***", Enum.GetNames<Restriction>())}***, ***All***.");
        }

    }
}
