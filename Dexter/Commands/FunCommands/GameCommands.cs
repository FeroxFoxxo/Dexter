using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Games;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Dexter.Helpers.Games;
using Discord;
using Discord.Commands;

namespace Dexter.Commands {
    partial class FunCommands {

        /// <summary>
        /// Interface Command for the games system and game management.
        /// </summary>
        /// <param name="Action">An action description of what to do in the system.</param>
        /// <param name="Arguments">A set of arguments to give context to <paramref name="Action"/>.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("game")]
        [Summary("Creates and manages game sessions. To create a game session use `~game <NEW|CREATE> [Game] [Title] ; [Description]`")]
        [ExtendedSummary("Creates and manages game sessions.\n" +
            "`<NEW|CREATE> [Game] [Title] (; [Description])` - Creates a new game instance and joins it as a Master.\n" +
            "`<HELP|INFO>` - Shows information for the game you're currently in.\n" +
            "`JOIN [GameID] (Password)` - Joins a game by its Game ID.\n" +
            "`LEAVE` - Leaves the game, if you're the master, this will also delete the session and kick all players.\n" +
            "`GET (GameID)` - Gets general info about a game session or yours.\n" +
            "`LEADERBOARD` - Displays the game leaderboard.\n" +
            "`LIST` - Displays a list of available games\n" +
            "`RESET` - Resets the game state.\n" +
            "`SAVE` - Forces a database save of the Dexter Games system.\n" +
            "`SET [Field] [Value]` - Sets a value for your game. \n" +
            "-  Common Fields are `password`, `title`, `master`, and `description`\n" +
            "-  Certain games have special fields, such as `term` or `maxlives` in Hangman.")]
        [Alias("games")]
        [BotChannel]

        public async Task GameCommand(string Action, [Remainder] string Arguments = "") {
            string Feedback;

            Player Player = GamesDB.GetOrCreatePlayer(Context.User.Id);
            GameInstance Game = null;
            if (Player.Playing > 0) {
                Game = GamesDB.Games.Find(Player.Playing);
                if (Game is not null && Game.Type == GameType.Unselected) Game = null;
            }

            switch (Action.ToLower()) {
                case "new":
                case "create":
                    string[] Args = Arguments.Split(" ");
                    if (Args.Length < 2) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid amount of parameters!")
                            .WithDescription("You must at least provide a game type and a title.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    string GameTypeStr = Args[0].ToLower();
                    if (!GameTypeConversion.GameNames.ContainsKey(GameTypeStr)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Game not found!")
                            .WithDescription($"Game \"{GameTypeStr}\" not found! Currently supported games are: {string.Join(", ", Enum.GetNames<GameType>()[1..])}")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    GameType GameType = GameTypeConversion.GameNames[GameTypeStr];
                    string RelevantArgs = Arguments[Args[0].Length..].Trim();
                    string Description = "";
                    string Title = RelevantArgs;

                    int SeparatorPos = RelevantArgs.IndexOf(';');
                    if (SeparatorPos + 1 == RelevantArgs.Length) SeparatorPos = -1;
                    if (SeparatorPos > 0) {
                        Title = RelevantArgs[..SeparatorPos];
                        Description = RelevantArgs[(SeparatorPos + 1)..];
                    }

                    Game = OpenSession(Player, Title.Trim(), Description.Trim(), GameType);
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"Created and Joined Game Session #{Game.GameID}!")
                        .WithDescription($"Created Game {Game.Title}.\nCurrently playing {Game.Type}")
                        .SendEmbed(Context.Channel);
                    return;
                case "help":
                case "info":
                    if (Game is null || Game.ToGameProper() is null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You aren't in a game!")
                            .WithDescription("Join or create a game before using this command.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    await Game.ToGameProper().Info(FunConfiguration).SendEmbed(Context.Channel);
                    return;
                case "leaderboard":
                case "points":
                    if (Game is null) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You aren't in a game!")
                            .WithDescription("Join or create a game before using this command.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    await Leaderboard(Game).SendEmbed(Context.Channel);
                    return;
                case "list":
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Supported Games!")
                        .WithDescription($"**{string.Join("\n", Enum.GetNames<GameType>()[1..])}**")
                        .SendEmbed(Context.Channel);
                    return;
                case "join":
                    if (string.IsNullOrEmpty(Arguments)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("No Arguments Provided!")
                            .WithDescription("You must provide a field and a value to set.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    string Number = Arguments.Split(" ")[0];
                    if (!int.TryParse(Number, out int ID)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Failed to parse Game ID")
                            .WithDescription($"Unable to parse {Number} into an integer value.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    if (Game is not null && Game.GameID == ID) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You're already in this game!")
                            .WithDescription("You can't join a game you're already in.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    Game = GamesDB.Games.Find(ID);
                    if (!Join(Player, Game, out Feedback, Arguments[Number.Length..].Trim())) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Failed to join game")
                            .WithDescription(Feedback)
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    GamesDB.SaveChanges();
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Success!")
                        .WithDescription(Feedback)
                        .SendEmbed(Context.Channel);
                    return;
                case "leave":
                    if (Player is null || Game is null) {
                        await Context.Message.ReplyAsync("You're not in a game!");
                        return;
                    }
                    RemovePlayer(Player);
                    GamesDB.SaveChanges();
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Left the game")
                        .WithDescription($"You left Game {Game.Title}. If you were the master, the session was closed.")
                        .SendEmbed(Context.Channel);
                    return;
                case "get":
                case "status":
                    int GameID = -1;
                    if (string.IsNullOrEmpty(Arguments) || !int.TryParse(Arguments, out GameID)) {
                        if (Game is null) {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Invalid selection")
                                .WithDescription("You're not in a game and no game was specified!")
                                .SendEmbed(Context.Channel);
                            return;
                        }
                    } else {
                        Game = GamesDB.Games.Find(GameID);
                    }
                    
                    if(Game is null || Game.ToGameProper() is null) {
                        await Context.Message.ReplyAsync("This game doesn't exist or isn't active!");
                        return;
                    }
                    await Game.ToGameProper().GetStatus(DiscordSocketClient).SendEmbed(Context.Channel);
                    return;
                case "reset":
                    if (Game is null || Game.ToGameProper() is null) {
                        await Context.Message.ReplyAsync("You're not in an implemented game!");
                        return;
                    }
                    if (Game.Master != Context.User.Id) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Missing permissions!")
                            .WithDescription($"Only the game master (<@{Game.Master}>) can reset the game!")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    Game.ToGameProper().Reset(FunConfiguration, GamesDB);
                    GamesDB.SaveChanges();
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Game successfully reset!")
                        .WithDescription($"Reset Game ${Game.Title} (#{Game.GameID}) to its default state.")
                        .SendEmbed(Context.Channel);
                    return;
                case "save":
                    GamesDB.SaveChanges();
                    await Context.Message.ReplyAsync("Saved games!");
                    return;
                case "set":
                    if (string.IsNullOrEmpty(Arguments)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("No Arguments Provided!")
                            .WithDescription("You must provide a field and a value to set.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    string Field = Arguments.Split(" ")[0];
                    if (!Set(Player, Game, Field, Arguments[Field.Length..].Trim(), out Feedback)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Error!")
                            .WithDescription(Feedback)
                            .SendEmbed(Context.Channel);
                        return;
                    }

                    GamesDB.SaveChanges();
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"Changed the value of {Field}")
                        .WithDescription(string.IsNullOrEmpty(Feedback) ? $"{Field}'s value has been modified to \"{Arguments[Field.Length..].Trim()}\"" : Feedback)
                        .SendEmbed(Context.Channel);
                    return;
            }
        }

        /// <summary>
        /// Manages players within the Dexter Games subsystem.
        /// </summary>
        /// <param name="Action">What to do to <paramref name="User"/>, possible values as BAN, UNBAN, KICK, PROMOTE or SET</param>
        /// <param name="User">The target User representing the Player to perform <paramref name="Action"/> on.</param>
        /// <param name="Arguments">Any other relevant information for <paramref name="Action"/>.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("player")]
        [Alias("players")]
        [Summary("Manages players in your game, use this command to kick, ban, promote, or set values of your players.")]
        [ExtendedSummary("Manages players in your game.\n" +
            "`BAN [PLAYER]` - Bans a player from your game.\n" +
            "`UNBAN [PLAYER]` - Removes a ban for a player.\n" +
            "`KICK [PLAYER]` - Kicks a player from your game, they can rejoin afterwards if they so desire.\n" +
            "`PROMOTE [PLAYER]` - Promotes a player to game master.\n" +
            "`SET [FIELD] [VALUE]` - Sets a field for a player to a given value\n" +
            "-  Common fields are `score` and `lives`.")]
        [BotChannel]

        public async Task PlayerCommand(string Action, IUser User, [Remainder] string Arguments = "") {
            Player Player = GamesDB.Players.Find(User.Id);
            if (Player is null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("The player you targeted doesn't exist.")
                    .WithDescription("Make sure the player you targeted is playing in your game session!")
                    .SendEmbed(Context.Channel);
                return;
            }

            Player Author = GamesDB.Players.Find(Context.User.Id);
            if (Author is null) {
                await Context.Message.ReplyAsync("You aren't playing any games!");
                return;
            }
            GameInstance Game = GamesDB.Games.Find(Author.Playing);
            if (Game is null || Game.Master != Context.User.Id) {
                await Context.Message.ReplyAsync("You must be a master in an active game to manage players!");
                return;
            }

            switch (Action.ToLower()) {
                case "ban":
                    if (!BanPlayer(Player, Game)) {
                        await Context.Message.ReplyAsync("This user is already banned from your game!");
                        return;
                    }
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Ban Registered")
                        .WithDescription($"Player {User.Mention} has been banned from {Game.Title}.")
                        .AddField("Banned Players", Game.BannedMentions.TruncateTo(500))
                        .SendEmbed(Context.Channel);
                    return;
                case "unban":
                    if(!UnbanPlayer(User.Id, Game)) {
                        await Context.Message.ReplyAsync("This player isn't banned in your game!");
                        return;
                    }
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Unbanned Player")
                        .WithDescription($"Player {User.Mention} has been unbanned from {Game.Title}.")
                        .AddField(!string.IsNullOrWhiteSpace(Game.Banned), "Banned Players", Game.BannedMentions.TruncateTo(500))
                        .SendEmbed(Context.Channel);
                    return;
                case "kick":
                    if (Player.Playing != Game.GameID) {
                        await Context.Message.ReplyAsync("This player isn't in your game!");
                        return;
                    }
                    RemovePlayer(Player);
                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Player successfully kicked")
                        .WithDescription($"Player {User.Mention} has been kicked from {Game.Title}.")
                        .SendEmbed(Context.Channel);
                    return;
                case "promote":
                    await GameCommand("set", $"master {User.Id}");
                    return;
                case "set":
                    string[] Args = Arguments.Split(" ");
                    if(Args.Length < 2) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Invalid number of arguments!")
                            .WithDescription("You must provide a field and a value to set it to.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    if(!double.TryParse(Args[1], out double Value)) {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Cannot parse value")
                            .WithDescription($"The term `{Args[1]}` cannot be parsed to a number.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    switch (Args[0].ToLower()) {
                        case "lives":
                            Player.Lives = (int) Value;
                            await Context.Message.ReplyAsync($"Player {User.Mention} now has {Value:D0} lives!");
                            return;
                        case "score":
                        case "points":
                            Player.Score = Value;
                            await Context.Message.ReplyAsync($"Player {User.Mention} not has a score of {Value:G3}!");
                            return;
                    }
                    return;
            }
        }

        private void RemovePlayer(ulong PlayerID) {
            Player Player = GamesDB.Players.Find(PlayerID);

            if (Player is null) return;
            RemovePlayer(Player);
        }

        private void RemovePlayer(Player Player) {
            int WasPlaying = Player.Playing;

            Player.Score = 0;
            Player.Lives = 0;
            Player.Data = "";
            Player.Playing = -1;

            CheckCloseSession(WasPlaying);
        }

        private void CheckCloseSession(int InstanceID) {
            GameInstance Instance = GamesDB.Games.Find(InstanceID);
            if (Instance is null) return;
            Player Master = GamesDB.GetOrCreatePlayer(Instance.Master);
            if (Master.Playing != InstanceID) {
                CloseSession(Instance);
                return;
            }
        }

        private bool BanPlayer(Player Player, GameInstance Instance) {
            if (Player is not null && Player.Playing == Instance.GameID) {
                RemovePlayer(Player);
            }

            if (Instance.Banned.Split(", ").Contains(Player.UserID.ToString())) return false;
            Instance.Banned += (Instance.Banned.TrimEnd().Length > 0 ? ", " : "")
                + Player.UserID.ToString();
            return true;
        }

        private bool UnbanPlayer(ulong PlayerID, GameInstance Instance) {
            if (Instance is null) return false;

            List<string> b = Instance.Banned.Split(", ").ToList();
            if (!b.Contains(PlayerID.ToString())) return false;
            b.Remove(PlayerID.ToString());
            Instance.Banned = string.Join(", ", b);
            return true;
        }

        private void CloseSession(GameInstance Instance) {
            Player[] Players = GamesDB.GetPlayersFromInstance(Instance.GameID);

            foreach(Player p in Players) {
                RemovePlayer(p);
            }

            GamesDB.Games.Remove(Instance);
        }

        private GameInstance OpenSession(Player Master, string Title, string Description, GameType GameType) {
            RemovePlayer(Master);

            GameInstance Result = new() {
                GameID = GamesDB.GenerateGameToken(),
                Master = Master.UserID,
                Title = Title,
                Description = Description,
                Type = GameType,
                Banned = "",
                Data = "",
                LastInteracted = DateTimeOffset.Now.ToUnixTimeSeconds(),
                LastUserInteracted = Master.UserID,
                Password = ""
            };

            GamesDB.Add(Result);
            Join(Master, Result, out _);
            GamesDB.SaveChanges();

            IGameTemplate Game = Result.ToGameProper();
            if (Game is not null) Game.Reset(FunConfiguration, GamesDB);
            return Result;
        }

        private bool Set(Player Player, GameInstance Instance, string Field, string Value, out string Feedback) {
            if(Player is null) {
                Feedback = "You are not registered in any game! Join a game before you attempt to set a value.";
                return false;
            }

            if(Instance is null) {
                Feedback = "You are not registered in any game! Join a game before you attempt to set a value.";
                return false;
            }

            if (Context.User.Id != Instance.Master) {
                Feedback = "You are not this game's Master! You can't modify its values.";
                return false;
            }

            switch(Field.ToLower()) {
                case "title":
                    Instance.Title = Value.Trim();
                    Feedback = $"Success! Title is \"{Value}\"";
                    return true;
                case "desc":
                case "description":
                    Instance.Description = Value.Trim();
                    Feedback = $"Success! Description is \"{Value}\"";
                    return true;
                case "password":
                    Instance.Password = Value.Trim();
                    Feedback = $"Success! Password is \"{Value}\"";
                    return true;
                case "master":
                    ulong ID;
                    IUser Master;
                    if(Context.Message.MentionedUsers.Count > 0) {
                        Master = Context.Message.MentionedUsers.First();
                        ID = Master.Id;
                    } else if (ulong.TryParse(Value, out ID)) {
                        Master = DiscordSocketClient.GetUser(ID);
                        if(Master is null) {
                            Feedback = $"The ID provided ({ID}) doesn't map to any user.";
                            return false;
                        }
                    } else {
                        Feedback = $"Unable to parse a user from \"{Value}\"!";
                        return false;
                    }
                    Player p = GamesDB.Players.Find(Master.Id);
                    if(p is null || p.Playing != Instance.GameID) {
                        Feedback = $"The player you targeted isn't playing in your game instance!";
                        return false;
                    }

                    Instance.Master = ID;
                    Feedback = $"Success! Master set to {Master.Mention}";
                    return true;
                default:
                    IGameTemplate Game = Instance.ToGameProper();
                    if (Game is null) {
                        Feedback = "Game mode is not set! This is weird... you should probably make a new game session";
                        return false;
                    }

                    return Game.Set(Field, Value, FunConfiguration, out Feedback); 
            }
        }

        private EmbedBuilder Leaderboard(GameInstance Instance) {
            StringBuilder Board = new StringBuilder();
            List<Player> Players = GamesDB.GetPlayersFromInstance(Instance.GameID).ToList();
            Players.Sort((a, b) => b.Score.CompareTo(a.Score));

            foreach (Player p in Players) {
                IUser User = DiscordSocketClient.GetUser(p.UserID);
                if (User is null) continue;
                Board.Append($"{(Board.Length > 0 ? "\n" : "")}{User.Username.TruncateTo(16),-16}| {p.Score:G4}, ♥×{p.Lives}");
            }

            return new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithTitle($"Leaderboard for {Instance.Title}")
                .WithDescription($"`{Board}`");
        }

        private bool Join(Player Player, GameInstance Instance, out string Feedback, string Password = "") {
            Feedback = "";
            
            if (Instance is null) {
                Feedback = "Game Instance does not exist.";
                return false;
            }

            string[] BannedIDs = Instance.Banned.Split(", ");
            if (!string.IsNullOrWhiteSpace(Instance.Banned)) {
                foreach (string s in BannedIDs) {
                    if (ulong.Parse(s) == Player.UserID) {
                        Feedback = "Player is banned from this game.";
                        return false;
                    }
                }
            }

            if (!string.IsNullOrEmpty(Instance.Password) && Password != Instance.Password) {
                Feedback = "Password is incorrect.";
                return false;
            }

            RemovePlayer(Player);
            Player.Playing = Instance.GameID;

            Feedback = $"Joined {Instance.Title} (Game #{Instance.GameID})";
            return true;
        }

    }
}
