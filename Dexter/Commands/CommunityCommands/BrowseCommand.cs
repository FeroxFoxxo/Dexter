using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.CommunityEvents;
using Dexter.Databases.Games;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Humanizer;

namespace Dexter.Commands {
    public partial class CommunityCommands {

        [Command("browse")]
        [Summary("Browse for games or events: `browse [Type] (Filters)`")]
        [ExtendedSummary("Browse for games or events!\n" +
            "`browse GAMES (Gametype)` - Browse for open game sessions, with an optional gametype.\n" +
            "`browse EVENTS (<OFFICIAL|COMMUNITY>)` - Browse for upcoming scheduled events!")]
        [BotChannel]

        public async Task BrowseCommand(string Type, [Remainder] string Filters = "") {
            EmbedBuilder[] Embeds;

            switch (Type.ToLower()) {
                case "games":
                    GameInstance[] Games;

                    if (string.IsNullOrEmpty(Filters)) {
                        Games = GamesDB.Games.ToArray();
                    }
                    else {
                        if (!GameTypeConversion.GameNames.ContainsKey(Type.ToLower())) {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Unable to find game type")
                                .WithDescription($"I wasn't able to parse \"{Filters}\" into a valid game type.\n" +
                                    $"Valid game types are {string.Join(", ", Enum.GetNames<GameType>()[1..])}")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                        Games = GamesDB.Games.AsQueryable().Where(g =>
                            g.Type == GameTypeConversion.GameNames[Type.ToLower()]).ToArray();
                    }

                    Embeds = BuildGamesEmbeds(Games);
                    break;
                case "events":
                    CommunityEvent[] Events;

                    if (string.IsNullOrEmpty(Filters)) {
                        Events = CommunityEventsDB.Events.AsQueryable().Where(e =>
                            e.Status == EventStatus.Approved).ToArray();
                    } 
                    else {
                        switch (Filters.ToLower()) {
                            case "official":
                                Events = CommunityEventsDB.Events.AsQueryable().Where(e =>
                                    e.EventType == EventType.Official
                                    && e.Status == EventStatus.Approved).ToArray();
                                break;
                            case "community":
                            case "userhosted":
                            case "user hosted":
                            case "user-hosted":
                                Events = CommunityEventsDB.Events.AsQueryable().Where(e =>
                                    e.EventType == EventType.UserHosted
                                    && e.Status == EventStatus.Approved).ToArray();
                                break;
                            default:
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Invalid Filter Parameter!")
                                    .WithDescription($"Unable to parse \"{Filters}\" into an event type. " +
                                        $"\nMake sure you use either \"official\" or \"community\".")
                                    .SendEmbed(Context.Channel);
                                return;
                        }
                    }

                    Embeds = BuildEventsEmbeds(Events);
                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Invalid Type Parameter!")
                        .WithDescription($"Unable to parse \"{Type}\" into a valid type." +
                            "\nPlease include what you're browsing for in the command, information about valid types can be found in the `help browse` command.")
                        .SendEmbed(Context.Channel);
                    return;
            }

            if(Embeds.Length == 0) {
                await BuildEmbed(EmojiEnum.Wut)
                    .WithTitle($"No {Type.ToLower()} found!")
                    .WithDescription("No items match your search criteria!")
                    .SendEmbed(Context.Channel);
            } else if (Embeds.Length == 1) {
                await Embeds[0].SendEmbed(Context.Channel);
            } else {
                await CreateReactionMenu(Embeds, Context.Channel);
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
    }
}
