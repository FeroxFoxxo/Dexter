using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Attributes.Methods;
using Dexter.Databases.CommunityEvents;
using Dexter.Databases.FunTopics;
using Dexter.Databases.Games;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Humanizer;

namespace Dexter.Commands {
    public partial class CommunityCommands {

        private const int MaxFieldContentsLength = 2000;

        /// <summary>
        /// Displays a list of related items of the given <paramref name="type"/> modified by <paramref name="filters"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="filters"></param>
        /// <returns>A <c>Task</c> object, which can be awaited until the program completes successfully.</returns>

        [Command("browse")]
        [Summary("Browse for games or events: `browse [Type] (Filters)`")]
        [ExtendedSummary("Browse for games or events!\n" +
            "`browse GAMES (Gametype)` - Browse for open game sessions, with an optional gametype.\n" +
            "`browse EVENTS (<OFFICIAL|COMMUNITY>)` - Browse for upcoming scheduled events!\n" +
            "`browse TOPICS [Expression]` - Browse topics similar to the given expression.")]
        [BotChannel]

        public async Task BrowseCommand(string type, [Remainder] string filters = "") {
            EmbedBuilder[] embeds;

            switch (type.ToLower()) {
                case "games":
                    GameInstance[] games;

                    if (string.IsNullOrEmpty(filters)) {
                        games = GamesDB.Games.ToArray();
                    }
                    else {
                        if (!GameTypeConversion.GameNames.ContainsKey(type.ToLower())) {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Unable to find game type")
                                .WithDescription($"I wasn't able to parse \"{filters}\" into a valid game type.\n" +
                                    $"Valid game types are {string.Join(", ", Enum.GetNames<GameType>()[1..])}")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                        games = GamesDB.Games.AsQueryable().Where(g =>
                            g.Type == GameTypeConversion.GameNames[type.ToLower()]).ToArray();
                    }

                    embeds = BuildGamesEmbeds(games);
                    break;
                case "events":
                    CommunityEvent[] events;

                    if (string.IsNullOrEmpty(filters)) {
                        events = CommunityEventsDB.Events.AsQueryable().Where(e =>
                            e.Status == EventStatus.Approved).ToArray();
                    } 
                    else {
                        switch (filters.ToLower()) {
                            case "official":
                                events = CommunityEventsDB.Events.AsQueryable().Where(e =>
                                    e.EventType == EventType.Official
                                    && e.Status == EventStatus.Approved).ToArray();
                                break;
                            case "community":
                            case "userhosted":
                            case "user hosted":
                            case "user-hosted":
                                events = CommunityEventsDB.Events.AsQueryable().Where(e =>
                                    e.EventType == EventType.UserHosted
                                    && e.Status == EventStatus.Approved).ToArray();
                                break;
                            default:
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Invalid Filter Parameter!")
                                    .WithDescription($"Unable to parse \"{filters}\" into an event type. " +
                                        $"\nMake sure you use either \"official\" or \"community\".")
                                    .SendEmbed(Context.Channel);
                                return;
                        }
                    }

                    embeds = BuildEventsEmbeds(events);
                    break;
                case "topics":
                    if(string.IsNullOrEmpty(filters)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Insufficient parameters!")
                            .WithDescription("Missing `filters` parameter; you must provide a term to search for!")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    List<WeightedObject> topics = new List<WeightedObject>();
                    foreach(FunTopic t in FunTopicsDB.Topics) {
                        topics.Add(new WeightedObject(t, LanguageHelper.GetCorrelationIndex(t.Topic, filters)));
                    }
                    WeightedObject.SortByWeightInPlace(topics, true);
                    embeds = new EmbedBuilder[0];
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Invalid Type Parameter!")
                        .WithDescription($"Unable to parse \"{type}\" into a valid type." +
                            "\nPlease include what you're browsing for in the command, information about valid types can be found in the `help browse` command.")
                        .SendEmbed(Context.Channel);
                    return;
            }

            await DisplayEmbeds(type, embeds);
        }

        private async Task DisplayEmbeds(string type, EmbedBuilder[] embeds) {
            if (embeds.Length == 0) {
                await BuildEmbed(EmojiEnum.Wut)
                    .WithTitle($"No {type.ToLower()} found!")
                    .WithDescription("No items match your search criteria!")
                    .SendEmbed(Context.Channel);
            } else if (embeds.Length == 1) {
                await embeds[0].SendEmbed(Context.Channel);
            } else {
                await CreateReactionMenu(embeds, Context.Channel);
            }
        }

        const int GamesPerEmbed = 15;

        private EmbedBuilder[] BuildGamesEmbeds(GameInstance[] Games) {
            if (Games.Length == 0) return new EmbedBuilder[0];
            EmbedBuilder[] Embeds = new EmbedBuilder[(Games.Length - 1) / GamesPerEmbed + 1];

            for(int i = 0; i < Embeds.Length; i++) {
                GameInstance[] RelevantGames = Games[(i * GamesPerEmbed)..((i + 1) * GamesPerEmbed > Games.Length ? Games.Length : (i + 1) * GamesPerEmbed)];

                Embeds[i] = new EmbedBuilder()
                    .WithColor(Color.Magenta)
                    .WithTitle($"Active Games - Page {i + 1}/{Embeds.Length}")
                    .WithDescription($"Here are a couple of games and some info about them, 'PC' is 'Player Count'. Each game type has an emoji to represent it!\n" +
                    $"{Header}\n" +
                    $"{StringifyGames(RelevantGames)}")
                    .WithFooter($"{i + 1}/{Embeds.Length}")
                    .WithCurrentTimestamp();
            }

            return Embeds;
        }

        const string Header = "`ID`|🎮|`     Title     `|`  Game Master  `|`PC`|🔒";
        private string StringifyGames(GameInstance[] Games) {
            
            StringBuilder Str = new StringBuilder();

            foreach(GameInstance Game in Games) {
                Str.Append(StringifyGame(Game) + "\n");
            }

            return Str.ToString().TrimEnd();
        }

        private string StringifyGame(GameInstance Game) {
            IGuildUser Master = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetUser(Game.Master);
            string MasterName;
            if (Master is null) MasterName = "Unknown";
            else MasterName = Master.Nickname ?? Master.Username;

            Player[] Players = GamesDB.GetPlayersFromInstance(Game.GameID);
                
            return $"`{Game.GameID:D2}`|{GameTypeConversion.GameEmoji[Game.Type]}|`{ToLength(15, Game.Title)}`|`{ToLength(15, MasterName)}`|`{Players.Length:D2}`|{(Game.Password.Length > 0 ? "🔑" : "")}";
        }

        private string ToLength(int Length, string S) {
            if (S.Length < Length) return S.PadRight(Length);
            return S.TruncateTo(Length);
        }

        const int EventsPerEmbed = 5;

        private EmbedBuilder[] BuildEventsEmbeds(CommunityEvent[] Events) {
            if (Events.Length == 0) return new EmbedBuilder[0];
            EmbedBuilder[] Embeds = new EmbedBuilder[(Events.Length - 1) / EventsPerEmbed + 1];
            int counter = 0;

            for (int i = 0; i < Embeds.Length; i++) {
                CommunityEvent[] RelevantEvents = Events[(i * GamesPerEmbed)..((i + 1) * GamesPerEmbed > Events.Length ? Events.Length : (i + 1) * GamesPerEmbed)];

                Embeds[i] = new EmbedBuilder()
                    .WithColor(Color.Magenta)
                    .WithTitle($"Upcoming Events - Page {i + 1}/{Embeds.Length}")
                    .WithDescription("Here are a couple of events:")
                    .WithFooter($"{i + 1}/{Embeds.Length}")
                    .WithCurrentTimestamp();

                foreach(CommunityEvent Event in RelevantEvents) {
                    TimeSpan TimeUntil = DateTimeOffset.FromUnixTimeSeconds(Event.DateTimeRelease).Subtract(DateTimeOffset.Now);
                    Embeds[i].AddField($"Event {++counter} (ID {Event.ID})", 
                        $"{Event.Description.TruncateTo(100)}\n" +
                        $"**Release**: {TimeUntil.Humanize(2, maxUnit: Humanizer.Localisation.TimeUnit.Year)} from now.");
                }
            }

            return Embeds;
        }

        private EmbedBuilder[] BuildTopicsEmbeds(List<FunTopic> topics) {

        }
    }
}
