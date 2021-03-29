using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Reminders;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;

namespace Dexter.Commands {
    partial class UtilityCommands {

        const string DateArgSeparator = ";";
        const string DateArgSeparatorName = "a semicolon";

        /// <summary>
        /// Creates or manages user-issued self-reminders.
        /// </summary>
        /// <param name="Action">A string expression for the action to take.</param>
        /// <param name="Arguments">A string expression containing the arguments for the command.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes execution successfully.</returns>

        [Command("reminder")]
        [Summary("Creates and manages reminders.\n" +
            "`ADD [Time] <;|to> [Reminder]` - Creates a new reminder for the target date and time.\n" +
            "`REMOVE [Reminder ID]` - Removes a previously created reminder by its ID.\n" +
            "`EDIT [Reminder ID] [New Text]` - Edits the text attached to an existing reminder.\n" +
            "`GET [Reminder ID]` - Gets the relevant information about a reminder." +
            "`UPCOMING` - Shows all upcoming reminders for the user, if any.")]
        [BotChannel]

        public async Task ReminderCommand(string Action, [Remainder] string Arguments = "") {
            Action = Action.ToLower();

            Reminder Reminder;

            switch (Action) { 
                case "add":
                    int SeparatorIndex = Arguments.IndexOf(DateArgSeparator);

                    if (SeparatorIndex == -1) {
                        System.Text.RegularExpressions.Match Match = System.Text.RegularExpressions.Regex.Match(Arguments, @"\sto\s");
                        if (Match.Success) {
                            SeparatorIndex = Match.Index;
                            Arguments = $"{Arguments[..Match.Index]};{Arguments[(Match.Index + Match.Length)..]}";
                        } else {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Unable to parse arguments!")
                                .WithDescription($"Make sure to separate your time and reminder with {DateArgSeparatorName} ({DateArgSeparator}) or \" to \".")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                    }

                    string DateSection = Arguments[..SeparatorIndex];
                    string Message = Arguments[(SeparatorIndex + 1)..].Trim();

                    if (!DateSection.TryParseTime(CultureInfo.CurrentCulture, LanguageConfiguration, out DateTimeOffset Date, out _)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable to parse date!")
                            .WithDescription($"Time {DateSection} cannot be parsed into a valid date.\nUse the `{BotConfiguration.Prefix}checktime` command to learn more about this.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    if (string.IsNullOrEmpty(Message)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Empty Reminder!")
                            .WithDescription("I think reminding you of nothing is kind of unnecessary, don't you think so?")
                            .SendEmbed(Context.Channel);
                        return;
                    } else if (Message.Length > 1024) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Your message is too long!")
                            .WithDescription("Try to keep your reminder under 1024 characters.")
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    Reminder = ReminderDB.AddReminder(Context.User, Date, Message);
                    ReminderDB.SaveChanges();

                    await CreateEventTimer(ReminderCallback, new Dictionary<string, string> { { "ID", Reminder.ID.ToString() } }, (int) Date.Subtract(DateTimeOffset.Now).TotalSeconds, Databases.EventTimers.TimerType.Expire);

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"🎗Created Reminder #{Reminder.ID}🎗")
                        .WithDescription($"The reminder will be released on {Date.HumanizeExtended()}")
                        .WithCurrentTimestamp()
                        .SendEmbed(Context.Channel);
                    return;
                case "remove":
                    Reminder = await TryParseAndGetReminder(Arguments, Context.User);
                    if (Reminder == null) return;

                    if (Reminder.Status != ReminderStatus.Pending) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Reminder is not Pending!")
                            .WithDescription($"This reminder has already been {Reminder.Status}, it can't be removed.")
                            .WithCurrentTimestamp()
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    Reminder.Status = ReminderStatus.Removed;
                    ReminderDB.SaveChanges();

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"Reminder #{Reminder.ID} successfully removed!")
                        .WithDescription($"You will no longer receive this reminder, but you can still check its information using the `{BotConfiguration.Prefix}reminder get {Reminder.ID}` command.")
                        .WithCurrentTimestamp()
                        .SendEmbed(Context.Channel);
                    return;
                case "edit":
                    string IDArg = Arguments.Split(" ").FirstOrDefault();
                    string NewMessage = Arguments[IDArg.Length..].Trim();

                    Reminder = await TryParseAndGetReminder(IDArg, Context.User);
                    if (Reminder == null) return;

                    if(Reminder.Status != ReminderStatus.Pending) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Reminder is not Pending!")
                            .WithDescription($"This reminder has already been {Reminder.Status}, it can't be edited.")
                            .WithCurrentTimestamp()
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    Reminder.Message = NewMessage;
                    ReminderDB.SaveChanges();

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"Reminder #{Reminder.ID} successfully edited!")
                        .WithDescription($"The reminder will now appear with the content defined above.")
                        .WithCurrentTimestamp()
                        .SendEmbed(Context.Channel);
                    return;
                case "get":
                    Reminder = await TryParseAndGetReminder(Arguments, Context.User);
                    if (Reminder == null) return;

                    await BuildReminderInfo(Reminder).SendEmbed(Context.Channel);
                    return;
                case "upcoming":
                    List<Reminder> Reminders = ReminderDB.GetRemindersByUser(Context.User)
                        .Where(r => r.Status == ReminderStatus.Pending)
                        .ToList();
                    Reminders.Sort((a1, a2) => a1.DateTimeRelease.CompareTo(a2.DateTimeRelease));
                    if(Reminders.Count == 0) {
                        await BuildEmbed(EmojiEnum.Wut)
                            .WithTitle("No upcoming reminders!")
                            .WithDescription("It seems as though you need no assistance with your mental storage allocation process for now!")
                            .WithCurrentTimestamp()
                            .SendEmbed(Context.Channel);
                    } else if(Reminders.Count == 1) {
                        await BuildReminderInfo(Reminders[0])
                            .SendEmbed(Context.Channel);
                    } else if(Reminders.Count <= UtilityConfiguration.ReminderMaxItemsPerPage) {
                        await BuildReminderPage(Reminders)
                            .SendEmbed(Context.Channel);
                    } else {
                        EmbedBuilder[] Embeds = BuildReminderEmbeds(Reminders);
                        await CreateReactionMenu(Embeds, Context.Channel);
                    }
                    return;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Action Parse Error!")
                        .WithDescription($"Action \"{Action}\" not found! Please use `ADD`, `REMOVE`, `EDIT`, or `UPCOMING`")
                        .SendEmbed(Context.Channel);
                    return;
            }
        }

        /// <summary>
        /// Shorthand of the reminder command used to add a new reminder.
        /// </summary>
        /// <param name="Arguments">The date and time separated from the reminder body by a DateArgSeparator.</param>
        /// <returns>A <c>Task</c> object, that can be awaited until the method completes successfully.</returns>

        [Command("remindme")]
        [Summary("Adds a reminder given a date and a reminder body")]
        [ExtendedSummary("Adds a reminder given a date and a reminder body separated by a semicolon\n" +
            "SYNTAX: `remindme [DATE] <;|to> [REMINDER]`\n" +
            "Examples:\n" +
            "    `remindme in 20 minutes to bathe Roxy (my cat)`\n" +
            "    `remindme June 11 2021 8:00 CDT to congratulate my friend for their birthday!`\n" +
            "    `remindme in 1d 4h; Gotta go to the dentist.`\n")]
        [BotChannel]

        public async Task RemindMeShorthandCommand([Remainder] string Arguments) {
            await ReminderCommand("add", Arguments);
        }

        /// <summary>
        /// Executes a reminder once its release time arrives.
        /// </summary>
        /// <param name="Args">A string-string Dictionary containing a definition for "ID", which must be parsable to an <c>int</c>.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully</returns>

        public async Task ReminderCallback(Dictionary<string, string> Args) {
            Reminder Reminder = ReminderDB.GetReminder(int.Parse(Args["ID"]));
            if (Reminder == null || Reminder.Status != ReminderStatus.Pending) return;

            IUser Issuer = DiscordSocketClient.GetUser(Reminder.IssuerID);
            if (Issuer == null) return;

            Reminder.Status = ReminderStatus.Released;
            ReminderDB.SaveChanges();

            try {
                await BuildEmbed(EmojiEnum.Sign)
                    .WithTitle("🎗Dexter Reminder!🎗")
                    .WithDescription(Reminder.Message)
                    .WithCurrentTimestamp()
                    .SendEmbed(await Issuer.GetOrCreateDMChannelAsync());
            }
            catch { }
        }

        private EmbedBuilder[] BuildReminderEmbeds(IEnumerable<Reminder> Reminders) {
            Reminder[] ReminderArray = Reminders.ToArray();
            int TotalPages = (ReminderArray.Length - 1) / UtilityConfiguration.ReminderMaxItemsPerPage + 1;

            EmbedBuilder[] Embeds = new EmbedBuilder[TotalPages];

            int counter = 1;

            for(int p = 1; p <= TotalPages; p++) {
                int First = (p - 1) * UtilityConfiguration.ReminderMaxItemsPerPage;
                int LastExcluded = p * UtilityConfiguration.ReminderMaxItemsPerPage;
                if (LastExcluded > ReminderArray.Length) LastExcluded = ReminderArray.Length;

                Embeds[p - 1] = BuildReminderPage(ReminderArray[First..LastExcluded], ref counter);

                Embeds[p - 1].WithTitle($"Reminders - Page {p}/{TotalPages}").WithFooter($"{p}/{TotalPages}");
            }

            return Embeds;
        }

        private EmbedBuilder BuildReminderPage(IEnumerable<Reminder> Reminders) {
            int count = 1;
            return BuildReminderPage(Reminders, ref count);
        }

        private EmbedBuilder BuildReminderPage(IEnumerable<Reminder> Reminders, ref int counter) {
            EmbedBuilder Builder = BuildEmbed(EmojiEnum.Sign)
                .WithTitle("Reminders")
                .WithCurrentTimestamp();

            foreach(Reminder r in Reminders) {
                Builder.AddField($"🎗Reminder {counter++} (ID {r.ID})🎗", $"{r.Message.Truncate(UtilityConfiguration.ReminderMaxCharactersPerItem)}\n " +
                    $"- **Release:** {DateTimeOffset.FromUnixTimeSeconds(r.DateTimeRelease).HumanizeExtended(BotConfiguration, true)}");
            }

            return Builder;
        }

        private EmbedBuilder BuildReminderInfo(Reminder Reminder) {
            return BuildEmbed(EmojiEnum.Sign)
                .WithTitle($"Reminder #{Reminder.ID}")
                .WithDescription(Reminder.Message)
                .AddField("Release:", $"{DateTimeOffset.FromUnixTimeSeconds(Reminder.DateTimeRelease).HumanizeExtended(BotConfiguration, true)}")
                .AddField("Status:", Reminder.Status.ToString());
        }

        private async Task<Reminder> TryParseAndGetReminder(string ID, IUser Issuer = null) {
            if(string.IsNullOrEmpty(ID)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("No ID was provided")
                    .WithDescription("You must provide a reminder ID for this command!")
                    .SendEmbed(Context.Channel);
                return null;
            }

            if (!int.TryParse(ID, out int Result)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Can't parse ID!")
                    .WithDescription($"ID {ID} cannot be parsed to a number.")
                    .SendEmbed(Context.Channel);
                return null;
            }

            Reminder r = ReminderDB.GetReminder(Result);

            if (r == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unknown Reminder")
                    .WithDescription($"No reminder exists with ID {Result}")
                    .SendEmbed(Context.Channel);
                return null;
            }

            if (Issuer != null && r.IssuerID != Issuer.Id) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Forbidden!")
                    .WithDescription("This reminder isn't yours, you're not allowed to access it.")
                    .SendEmbed(Context.Channel);
                return null;
            }

            return r;
        }
    }
}
