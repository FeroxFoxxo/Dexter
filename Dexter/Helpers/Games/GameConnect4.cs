using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Helpers.Games {
    
    /// <summary>
    /// Represents an instance of a tic tac toe game.
    /// </summary>

    public class GameConnect4 : IGameTemplate {

        /// <summary>
        /// The game instance that this specific game is attached to.
        /// </summary>

        public GameInstance game;

        /// <summary>
        /// Creates a new instance of a hangman game based on a generic game instance.
        /// </summary>
        /// <param name="game">The generic game instance to inherit from.</param>

        public GameConnect4(GameInstance game) {
            this.game = game;
            if(string.IsNullOrWhiteSpace(game.Data)) game.Data = EmptyData;
        }

        //Data structure: "term, guess, lives, maxlives, lettersmissed";
        const string EmptyData = "-------/-------/-------/-------/-------/-------, 0, 0, 0, Y";
        const string EmptyBoard = "-------/-------/-------/-------/-------/-------";

        private string StrState {
            get {
                return game.Data.Split(", ")[0];
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[0] = value;
                game.Data = string.Join(", ", newValue);
            }
        }

        private char[,] State { 
            get {
                char[,] result = new char[6, 7];
                string[] raw = StrState.Split('/');
                for (int i = 0; i < 6; i++) {
                    for (int j = 0; j < 7; j++)
                        result[i, j] = raw[i][j];
                }
                return result;
            }
            set {
                StringBuilder builder = new StringBuilder(60);
                for (int i = 0; i < 6; i++) {
                    for (int j = 0; j < 7; j++)
                        builder.Append(value[i, j]);
                    if (i != 5) builder.Append('/');
                }
                StrState = builder.ToString();
            }
        }

        private ulong BoardID {
            get {
                return ulong.Parse(game.Data.Split(", ")[1]);
            }
            set {
                string processedValue = value.ToString();
                string[] newValue = game.Data.Split(", ");
                newValue[1] = processedValue;
                game.Data = string.Join(", ", newValue);
            }
        }

        private ulong PlayerRed {
            get {
                return ulong.Parse(game.Data.Split(", ")[2]);
            }
            set {
                string processedValue = value.ToString();
                string[] newValue = game.Data.Split(", ");
                newValue[2] = processedValue;
                game.Data = string.Join(", ", newValue);
            }
        }

        private ulong PlayerYellow {
            get {
                return ulong.Parse(game.Data.Split(", ")[3]);
            }
            set {
                string processedValue = value.ToString();
                string[] newValue = game.Data.Split(", ");
                newValue[3] = processedValue;
                game.Data = string.Join(", ", newValue);
            }
        }

        private char Turn {
            get {
                return game.Data.Split(", ")[4][0];
            }
            set {
                string processedValue = value.ToString();
                string[] newValue = game.Data.Split(", ");
                newValue[4] = processedValue;
                game.Data = string.Join(", ", newValue);
            }
        }

        /// <summary>
        /// Represents the general status and data of a Hangman Game.
        /// </summary>
        /// <param name="client">SocketClient used to parse UserIDs.</param>
        /// <returns>An Embed detailing the various aspects of the game in its current instance.</returns>

        public EmbedBuilder GetStatus(DiscordSocketClient client) {
            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle($"{game.Title} (Game {game.GameID})")
                .WithDescription($"{game.Description}\n{DisplayState()}")
                .AddField($"{YellowChar} Player", $"{(PlayerYellow == default ? "-" : $"<@{PlayerYellow}>")}", true)
                .AddField($"{RedChar} Player", $"{(PlayerRed == default ? "-" : $"<@{PlayerRed}>")}", true)
                .AddField($"Turn", $"{ToEmoji[Turn]}", true)
                .AddField("Master", client.GetUser(game.Master).GetUserInformation())
                .AddField(game.Banned.Length > 0, "Banned Players", game.BannedMentions.TruncateTo(500));
        }

        const string EChar = "⚫️";
        const string YellowChar = "🟡";
        const string RedChar = "🔴";
        private readonly Dictionary<char, string> ToEmoji = new Dictionary<char, string>() {
            {'-', EChar},
            {'Y', YellowChar},
            {'R', RedChar}
        };

        private string DisplayState() {
            char[,] icons = State;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 6; i++) {
                builder.Append('|');
                for (int j = 0; j < 7; j++)
                    builder.Append(ToEmoji[icons[i, j]]);
                builder.Append($"|{(i == 5 ? ToEmoji[Turn] : "\n")}");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Resets the game state to its initial default value.
        /// </summary>
        /// <param name="funConfiguration">Settings related to the fun module, which contain the default lives parameter.</param>
        /// <param name="gamesDB">The database containing player information, set to <see langword="null"/> to avoid resetting scores.</param>

        public void Reset(FunConfiguration funConfiguration, GamesDB gamesDB) {
            game.Data = EmptyData;
            game.LastUserInteracted = game.Master;
            if (gamesDB is null) return;
            Player[] players = gamesDB.GetPlayersFromInstance(game.GameID);
            foreach(Player p in players) {
                p.Score = 0;
                p.Lives = 0;
            }
        }

        /// <summary>
        /// Sets a local <paramref name="field"/> to a given <paramref name="value"/>.
        /// </summary>
        /// <remarks>No Valid values for <paramref name="field"/>.</remarks>
        /// <param name="field">The name of the field to modify.</param>
        /// <param name="value">The value to set the field to.</param>
        /// <param name="funConfiguration">The Fun Configuration settings file, which holds relevant data such as default lives.</param>
        /// <param name="feedback">In case this operation wasn't possible, its reason, or useful feedback even if the operation was successful.</param>
        /// <returns><see langword="true"/> if the operation was successful, otherwise <see langword="false"/>.</returns>

        public bool Set(string field, string value, FunConfiguration funConfiguration, out string feedback) {
            feedback = $"Connect 4 doesn't implement any special fields! {field} isn't a valid default field.";
            return false;
        }

        /// <summary>
        /// Gives general information about the game and how to play it.
        /// </summary>
        /// <param name="funConfiguration"></param>
        /// <returns></returns>

        public EmbedBuilder Info(FunConfiguration funConfiguration) {
            return new EmbedBuilder()
                .WithColor(Color.Magenta)
                .WithTitle("How To Play: Connect 4")
                .WithDescription("**Step 1:** Create a new board by typing `board`.\n" +
                    "**Step 2:** Claim your token, type `claim <Y|R>` to claim Yellow or Red respectively.\n" +
                    "**Step 3:** `Y` starts! type `[POS]` to drop your token at a given position.\n" +
                    "Positions are the following:\n" +
                    "```\n" +
                    "1 2 3 4 5 6 7\n" +
                    "↓ ↓ ↓ ↓ ↓ ↓ ↓\n" +
                    "```\n" +
                    "Keep playing until you fill the board or get a line of four.\n" +
                    "You can pass the token by typing `pass <Y|R> [Player]` to give control of a token to another player (only the master or the player with that token can do this).\n" +
                    "Alternatively, the master can type `swap` to swap player tokens, essentially exchanging turns.");
        }

        private bool PlaceToken(int x, char token) {
            char[,] state = State; int y;
            x--;
            for(y = 5; y >= -1; y--) {
                if (y == -1) return false;
                if (state[y, x] == '-') break;
            }
            state[y, x] = token;
            State = state;
            return true;
        }

        private bool CheckWin() {
            char[,] state = State;
            int count = 0;
            char pattern = '-';
            for(int y = 0; y < 6; y++) {
                for(int x = 0; x < 7; x++) {
                    if (state[y, x] == pattern) count++;
                    else {
                        count = 1;
                        pattern = state[y, x];
                    }
                    if (count == 4 && pattern != '-') return true;
                }
                count = 0;
            }
            for (int x = 0; x < 7; x++) {
                for (int y = 0; y < 6; y++) {
                    if (state[y, x] == pattern) count++;
                    else {
                        count = 1;
                        pattern = state[y, x];
                    }
                    if (count == 4 && pattern != '-') return true;
                }
                count = 0;
            }
            for (int xbase = -2; xbase < 4; xbase++) {
                int x = xbase;
                for (int y = 0; y < 6; y++) {
                    if (x < 0 || x >= 7) { x++; continue; }
                    if (state[y, x] == pattern) count++;
                    else {
                        count = 1;
                        pattern = state[y, x];
                    }
                    if (count == 4 && pattern != '-') return true;
                    x++;
                }
                count = 0;
            }
            for (int xbase = -2; xbase < 4; xbase++) {
                int x = xbase;
                for (int y = 5; y >= 0; y--) {
                    if (x < 0 || x >= 7) { x++; continue; }
                    if (state[y, x] == pattern) count++;
                    else {
                        count = 1;
                        pattern = state[y, x];
                    }
                    if (count == 4 && pattern != '-') return true;
                    x++;
                }
                count = 0;
            }
            return false;
        }

        private bool CheckDraw() {
            char[,] state = State;
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 7; j++) {
                    if (state[i, j] == '-') return false;
                }
            return true;
        }

        /// <summary>
        /// Handles a message sent by a player in the appropriate channel.
        /// </summary>
        /// <param name="message">The message context from which the author and content can be obtained.</param>
        /// <param name="gamesDB">The games database in case any player data has to be modified.</param>
        /// <param name="client">The Discord client used to parse users.</param>
        /// <param name="funConfiguration">The configuration file containing relevant game information.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task HandleMessage(IMessage message, GamesDB gamesDB, DiscordSocketClient client, FunConfiguration funConfiguration) {

            Player player = gamesDB.GetOrCreatePlayer(message.Author.Id);

            string msg = message.Content.ToUpper();
            IUserMessage board = null;
            if (BoardID != 0) board = await message.Channel.GetMessageAsync(BoardID) as IUserMessage;

            if(msg == "BOARD") {
                if (board is not null) await board.DeleteAsync();
                IUserMessage newBoard = await message.Channel.SendMessageAsync(DisplayState());
                BoardID = newBoard.Id;
                return;
            }

            string[] args = msg.Split(" ");
            if(msg.StartsWith("CLAIM")) {
                if(args.Length > 1) {
                    Player prevPlayer = null;
                    bool skip = false;
                    switch(args[1]) {
                        case "YELLOW":
                        case "Y":
                            if (PlayerYellow == 0) skip = true;
                            else prevPlayer = gamesDB.Players.Find(PlayerYellow);
                            break;
                        case "RED":
                        case "R":
                            if (PlayerRed == 0) skip = true;
                            else prevPlayer = gamesDB.Players.Find(PlayerRed);
                            break;
                        default:
                            await message.Channel.SendMessageAsync($"\"{args[1]}\" is not a valid token! Use 'Y' or 'R'.");
                            return;
                    }

                    if(!skip && prevPlayer is not null && prevPlayer.Playing == game.GameID) {
                        await message.Channel.SendMessageAsync($"Can't claim token since player <@{prevPlayer.UserID}> is actively controlling it.");
                        return;
                    }

                    if (args[1].StartsWith("R")) PlayerRed = message.Author.Id;
                    else PlayerYellow = message.Author.Id;

                    await message.Channel.SendMessageAsync($"<@{message.Author.Id}> will play with {(args[1].StartsWith("Y") ? "yellow" : "red")}!");
                    return;
                }
                await message.Channel.SendMessageAsync("You need to specify what token you'd like to claim!");
                return;
            }

            if(int.TryParse(args[0], out int pos)) {
                if (board is null) {
                    await message.Channel.SendMessageAsync($"You must create a board first! Type `board`");
                    return;
                }

                if ((Turn == 'R' && message.Author.Id != PlayerRed) || (Turn == 'Y' && message.Author.Id != PlayerYellow)) {
                    await message.Channel.SendMessageAsync($"You don't control the {(Turn == 'Y' ? "yellow" : "red")} tokens!");
                    return;
                }

                if(pos < 1 || pos > 7) {
                    await message.Channel.SendMessageAsync($"Position {pos} is not valid! It must be between 1 and 7.");
                    return;
                }

                if (!PlaceToken(pos, Turn)) {
                    await message.Channel.SendMessageAsync($"This position has no free spaces!");
                    return;
                }

                await message.DeleteAsync();
                Turn = Turn == 'Y' ? 'R' : 'Y';
                await board.ModifyAsync(m => m.Content = DisplayState());

                if (CheckWin()) {
                    Turn = Turn == 'Y' ? 'R' : 'Y';
                    BoardID = 0;
                    StrState = EmptyBoard;
                    player.Score += 1;
                    await new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle($"{ToEmoji[Turn]} wins!")
                        .WithDescription("Create a new board if you wish to play again, or pass your token control to a different player.")
                        .SendEmbed(message.Channel);
                    Turn = 'Y';
                    return;
                }

                if (CheckDraw()) {
                    Turn = Turn == 'Y' ? 'R' : 'Y';
                    BoardID = 0;
                    StrState = EmptyBoard;
                    Turn = 'Y';
                    await new EmbedBuilder()
                        .WithColor(Color.LightOrange)
                        .WithTitle("Draw!")
                        .WithDescription("No more tokens can be placed. Create a new board if you wish to play again, or pass your token control to a different player.")
                        .SendEmbed(message.Channel);
                    return;
                }

                return;
            }

            if(args.Length > 2 && args[0] == "PASS") {
                ulong otherID = message.MentionedUserIds.FirstOrDefault();
                if (otherID == default && !ulong.TryParse(args[2], out otherID) || otherID == 0) {
                    await message.Channel.SendMessageAsync($"Could not parse \"{args[2]}\" into a valid user.");
                    return;
                }
                IUser otherUser = client.GetUser(otherID);
                if (otherUser is null) {
                    await message.Channel.SendMessageAsync($"I wasn't able to find this user!");
                    return;
                }
                Player otherPlayer = gamesDB.GetOrCreatePlayer(otherUser.Id);
                if (otherPlayer.Playing != game.GameID) {
                    await message.Channel.SendMessageAsync("That user isn't playing in this game session!");
                    return;
                }

                switch(args[1]) {
                    case "R":
                    case "RED":
                        if(message.Author.Id != game.Master && message.Author.Id != PlayerRed) {
                            await message.Channel.SendMessageAsync($"You aren't the master nor controlling the {RedChar} token!");
                            return;
                        }
                        PlayerRed = otherPlayer.UserID;
                        break;
                    case "Y":
                    case "YELLOW":
                        if (message.Author.Id != game.Master && message.Author.Id != PlayerYellow) {
                            await message.Channel.SendMessageAsync($"You aren't the master nor controlling the {YellowChar} token!");
                            return;
                        }
                        PlayerYellow = otherPlayer.UserID;
                        break;
                    default:
                        await message.Channel.SendMessageAsync($"Unable to parse {args[1]} into a valid token!");
                        return;
                }

                await message.Channel.SendMessageAsync($"{otherUser.Mention} now controls token {(args[1][0] == 'Y' ? YellowChar : RedChar)}!");
                return;
            }

            if (args[0] == "SWAP") {
                if (message.Author.Id != game.Master) {
                    await message.Channel.SendMessageAsync("Only the game master can swap the tokens!");
                    return;
                }

                ulong temp = PlayerRed;
                PlayerRed = PlayerYellow;
                PlayerYellow = temp;
                await message.Channel.SendMessageAsync($"Tokens have been swapped!\n" +
                    $"{RedChar}: <@{PlayerRed}>\n" +
                    $"{YellowChar}: <@{PlayerYellow}>");
                return;
            }
        }
    }
}
