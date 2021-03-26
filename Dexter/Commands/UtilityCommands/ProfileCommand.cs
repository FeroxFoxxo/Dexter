using Dexter.Enums;
using Discord;
using Discord.Commands;
using Humanizer;
using System;
using System.Threading.Tasks;
using Dexter.Extensions;
using System.Runtime.InteropServices;
using Dexter.Databases.UserProfiles;
using Humanizer.Localisation;
using Discord.WebSocket;
using System.Linq;
using Dexter.Attributes.Methods;
using Dexter.Helpers;
using Dexter.Databases.Infractions;
using System.Collections.Generic;
using System.Text;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        /// <summary>
        /// Sends information concerning the profile of a target user.
        /// This information contains: Username, nickname, account creation and latest join date, and status.
        /// </summary>
        /// <param name="User">The target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("profile", RunMode = RunMode.Async)]
        [Summary("Gets the profile of the user mentioned or yours.")]
        [Alias("userinfo")]
        [BotChannel]

        public async Task ProfileCommand([Optional] IUser User) {
            if (User == null)
                User = Context.User;

            IGuildUser GuildUser = await DiscordSocketClient.Rest.GetGuildUserAsync(Context.Guild.Id, User.Id);
            DateTimeOffset Joined = UserRecordsService.GetUserJoin(GuildUser);

            UserProfile Profile = ProfilesDB.Profiles.Find(User.Id);

            await BuildEmbed(EmojiEnum.Unknown)
                .WithTitle($"User Profile For {GuildUser.Username}#{GuildUser.Discriminator}")
                .WithThumbnailUrl(GuildUser.GetTrueAvatarUrl())
                .AddField("Username", GuildUser.GetUserInformation())
                .AddField(!string.IsNullOrEmpty(GuildUser.Nickname), "Nickname", GuildUser.Nickname)
                .AddField("Created", $"{GuildUser.CreatedAt:MM/dd/yyyy HH:mm:ss} ({(DateTime.Now - GuildUser.CreatedAt.DateTime).Humanize(2, maxUnit: TimeUnit.Year)} ago)")
                .AddField(Joined != default, "Joined", $"{Joined:MM/dd/yyyy HH:mm:ss} ({DateTimeOffset.Now.Subtract(Joined).Humanize(2, maxUnit: TimeUnit.Year)} ago)")
                .AddField(Profile != null && Profile.BorkdayTime != default, "Last Birthday", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Profile != null ? Profile.BorkdayTime : 0).ToLongDateString())
                .AddField("Top Role", Context.Guild.Roles.Where(Role => Role.Position == Context.Guild.GetUser(User.Id).Hierarchy).FirstOrDefault().Name)
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Gets a list of nicknames given a <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The user whose nicknames are to be queried.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("nicknames")]
        [Summary("Gets the nicknames a user has had over time.")]
        [BotChannel]

        public async Task NicknamesCommand([Optional] IUser User) {
            await RunNamesCommands(User, NameType.Nickname);
        }

        /// <summary>
        /// Gets a list of usernames given a <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The user whose usernames are to be queried.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("usernames")]
        [Summary("gets the usernames a user has had over time.")]
        [BotChannel]

        public async Task UsernamesCommand([Optional] IUser User) {
            await RunNamesCommands(User, NameType.Username);
        }

        private async Task RunNamesCommands(IUser User, NameType NameType) {
            if (User == null)
                User = Context.User;

            List<NameRecord> Names = UserRecordsService.GetNameRecords(User, NameType).ToList();
            Names.Sort((a, b) => b.SetTime.CompareTo(a.SetTime));

            await BuildEmbed(EmojiEnum.Unknown)
                .WithTitle($"{NameType}s History for User {User.Username}#{User.Discriminator}")
                .WithThumbnailUrl(User.GetTrueAvatarUrl())
                .WithDescription(LanguageHelper.Truncate(string.Join(", ", Names), 2000))
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Removes Names from the database given a set of arguments to match for.
        /// </summary>
        /// <param name="User">The User to match for, only names whose UserID match this user's will be removed.</param>
        /// <param name="NameType">Whether to remove USERNAMEs or NICKNAMEs.</param>
        /// <param name="Arguments">Optionally include parsing mode (reg or lit) plus the expression to look for in each specific name.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("clearnames")]
        [Summary("Removes certain nicknames from a user's record\n" +
            "Usage: `clearnames [User] <NICK|USER> (<LIT|REG>) [Name]`")]
        [ExtendedSummary("Removes certain nicknames from a user's record\n" +
            "Usage: `clearnames [User] <NICK|USER> (<LIT|REG>) [Name]`\n" +
            "- <NICK|USER> represents whether to remove nicknames or usernames." +
            "- <LIT|REG> represents whether to use the literal Name or interpret it as a regular expression (advanced). Interpreted as \"LIT\" if omitted.")]
        [BotChannel]
        [RequireModerator]

        public async Task ClearNamesCommand(IUser User, string NameType, [Remainder] string Arguments) {
            NameType EnumNameType;
            
            switch(NameType.ToLower()) {
                case "nick":
                case "nickname":
                    EnumNameType = Databases.UserProfiles.NameType.Nickname;
                    break;
                case "user":
                case "username":
                    EnumNameType = Databases.UserProfiles.NameType.Username;
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to parse Name Type argument!")
                        .WithDescription($"Couldn't parse expression \"{NameType}\", make sure you use either NICK or USER, optionally followed by NAME")
                        .SendEmbed(Context.Channel);
                    return;
            }

            bool IsRegex = false;
            string Term = Arguments.Split(" ").FirstOrDefault().ToLower();
            string Name;

            switch (Term) {
                case "reg":
                    IsRegex = true;
                    Name = Arguments[Term.Length..].Trim();
                    break;
                case "lit":
                    Name = Arguments[Term.Length..].Trim();
                    break;
                default:
                    Name = Arguments;
                    break;
            }

            if(string.IsNullOrEmpty(Name)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("You must provide a name!")
                    .WithDescription("You didn't provide any name to remove!")
                    .SendEmbed(Context.Channel);
                return;
            }

            List<NameRecord> Removed = UserRecordsService.RemoveNames(User, EnumNameType, Name, IsRegex);

            if(Removed.Count == 0) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("No names found following your query.")
                    .WithDescription($"I wasn't able to find any name \"{Name}\" for this user! Make sure what you typed is correctly capitalized.")
                    .SendEmbed(Context.Channel);
                return;
            } else {
                Removed.Sort((a, b) => a.Name.CompareTo(b.Name));

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Names successfully deleted!")
                    .WithDescription($"This user had {Removed.Count} name{(Removed.Count != 1 ? "s" : "")} following this pattern:\n" +
                        $"{LanguageHelper.Truncate(string.Join(", ", Removed).ToString(), 2000)}")
                    .SendEmbed(Context.Channel);
                return;
            }
             
        }

    }

}
