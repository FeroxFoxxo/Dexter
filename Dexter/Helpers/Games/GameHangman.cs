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
    /// Represents an instance of a hangman game.
    /// </summary>

    public class GameHangman : IGameTemplate {

        /// <summary>
        /// The game instance that this specific game is attached to.
        /// </summary>

        public GameInstance Game;

        /// <summary>
        /// Creates a new instance of a hangman game based on a generic game instance.
        /// </summary>
        /// <param name="Game">The generic game instance to inherit from.</param>

        public GameHangman(GameInstance Game) {
            this.Game = Game;
            if(string.IsNullOrWhiteSpace(Game.Data)) Game.Data = EmptyData;
        }

        //Data structure: "term, guess, lives, maxlives, lettersmissed";
        const string EmptyData = "Default, _______, 6, 6, ";

        private string Term { 
            get {
                return Game.Data.Split(", ")[0].Replace(GameInstance.CommaRepresentation, ",");
            }
            set {
                string ProcessedValue = value.Replace(",", GameInstance.CommaRepresentation);
                string[] NewValue = Game.Data.Split(", ");
                NewValue[0] = ProcessedValue;
                Game.Data = string.Join(", ", NewValue);
            }
        }

        private string Guess {
            get {
                return Game.Data.Split(", ")[1].Replace(GameInstance.CommaRepresentation, ",");
            }
            set {
                string ProcessedValue = value.Replace(",", GameInstance.CommaRepresentation);
                string[] NewValue = Game.Data.Split(", ");
                NewValue[1] = ProcessedValue;
                Game.Data = string.Join(", ", NewValue);
            }
        }

        private int Lives {
            get {
                return int.Parse(Game.Data.Split(", ")[2]);
            }
            set {
                if (value < 0) return;
                if (value > MaxLives) MaxLives = value;
                string ProcessedValue = value.ToString();
                string[] NewValue = Game.Data.Split(", ");
                NewValue[2] = ProcessedValue;
                Game.Data = string.Join(", ", NewValue);
            }
        }

        private int MaxLives {
            get {
                return int.Parse(Game.Data.Split(", ")[3]);
            }
            set {
                if (value < 1) return;
                string ProcessedValue = value.ToString();
                string[] NewValue = Game.Data.Split(", ");
                NewValue[3] = ProcessedValue;
                Game.Data = string.Join(", ", NewValue);
                if (Lives > value) Lives = value;
            }
        }

        private string LettersMissed {
            get {
                string s = Game.Data.Split(", ")[4];
                if (s == "-") return "";
                return s;
            }
            set {
                string[] NewValue = Game.Data.Split(", ");
                NewValue[4] = value.Length == 0 ? "-" : value;
                Game.Data = string.Join(", ", NewValue);
            }
        }

        /// <summary>
        /// Represents the general status and data of a Hangman Game.
        /// </summary>
        /// <param name="Client">SocketClient used to parse UserIDs.</param>
        /// <returns>An Embed detailing the various aspects of the game in its current instance.</returns>

        public EmbedBuilder GetStatus(DiscordSocketClient Client) {
            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle($"{Game.Title} (Game {Game.GameID})")
                .WithDescription($"{Game.Description}\n**Term**: {DiscordifyGuess()}")
                .AddField("Lives", LivesExpression(), true)
                .AddField("Wrong Guesses", MistakesExpression(), true)
                .AddField("Master", Client.GetUser(Game.Master).GetUserInformation())
                .AddField(Game.Banned.Length > 0, "Banned Players", Game.BannedMentions.Truncate(500));
        }

        private void RegisterMistake(char c) {
            Lives--;
            LettersMissed += c;
        }

        private string DiscordifyGuess() {
            char[] Treated = Guess.Replace(" ", "/")
                .ToCharArray();
            return string.Join(" ", Treated).Replace("_", "\\_");
        }

        private static string ObscureTerm(string Term) {
            char[] Chars = Term.ToCharArray();
            for (int i = 0; i < Chars.Length; i++) {
                if (char.IsLetter(Chars[i])) Chars[i] = '_';
            }
            return string.Join("", Chars);
        }

        const char LifeFullChar = '♥';
        const char LifeEmptyChar = '☠';
        private string LivesExpression() {
            char[] Expression = new char[MaxLives];
            for(int i = 0; i < MaxLives; i++) {
                Expression[i] = i < Lives ? LifeFullChar : LifeEmptyChar;
            }
            return string.Join("", Expression);
        }

        private string MistakesExpression() {
            string Missed = string.Join(", ", LettersMissed);
            if (Missed.Length == 0) return "-";
            return Missed.ToString();
        }

        /// <summary>
        /// Resets the game state to its initial default value.
        /// </summary>
        /// <param name="FunConfiguration">Settings related to the fun module, which contain the default lives parameter.</param>

        public void Reset(FunConfiguration FunConfiguration) {
            Game.Data = EmptyData;
            Game.LastUserInteracted = Game.Master;
            Lives = MaxLives = FunConfiguration.HangmanDefaultLives;
        }

        const int MAX_LIVES_ALLOWED = 20;

        /// <summary>
        /// Sets a local <paramref name="Field"/> to a given <paramref name="Value"/>.
        /// </summary>
        /// <remarks>Valid <paramref name="Field"/> values are: TERM, LIVES, MAXLIVES, and MISTAKES.</remarks>
        /// <param name="Field">The name of the field to modify.</param>
        /// <param name="Value">The value to set the field to.</param>
        /// <param name="FunConfiguration">The Fun Configuration settings file, which holds relevant data such as default lives.</param>
        /// <param name="Feedback">In case this operation wasn't possible, its reason, or useful feedback even if the operation was successful.</param>
        /// <returns><see langword="true"/> if the operation was successful, otherwise <see langword="false"/>.</returns>

        public bool Set(string Field, string Value, FunConfiguration FunConfiguration, out string Feedback) {
            Feedback = "";
            int N;
            
            switch(Field.ToLower()) {
                case "word":
                case "term":
                    if(Value.Contains('_')) {
                        Feedback = "The term cannot contains underscores!";
                        return false;
                    } else if (Value.Length > 256) {
                        Feedback = "The term is too long!";
                        return false;
                    }
                    Reset(FunConfiguration);
                    Term = Value;
                    Guess = ObscureTerm(Value);
                    Feedback = $"Success! Term = {Value}, Guess = \"{DiscordifyGuess()}\"";
                    return true;
                case "lives":
                    if(!int.TryParse(Value, out N)) {
                        Feedback = $"Unable to parse {Value} into an integer value.";
                        return false;
                    }
                    if(N > MAX_LIVES_ALLOWED) {
                        Feedback = $"Too many lives! Please keep it below {MAX_LIVES_ALLOWED}";
                        return false;
                    }
                    Lives = N;
                    Feedback = $"Lives set to {Lives}/{MaxLives}";
                    return true;
                case "maxlives":
                    if (!int.TryParse(Value, out N)) {
                        Feedback = $"Unable to parse {Value} into an integer value.";
                        return false;
                    }
                    if (N > MAX_LIVES_ALLOWED) {
                        Feedback = $"Too many lives! Please keep it below {MAX_LIVES_ALLOWED}";
                        return false;
                    }
                    MaxLives = N;
                    Feedback = $"Lives set to {Lives}/{MaxLives}";
                    return true;
                case "missed":
                case "mistakes":
                    if (Value.ToLower() == "none") {
                        LettersMissed = "";
                        Feedback = "Reset mistakes.";
                        return true; 
                    }

                    HashSet<char> Missed = new HashSet<char>();
                    foreach(char c in Value.ToCharArray()) {
                        if (!char.IsLetter(c)) Missed.Add(char.ToUpper('c'));
                    }
                    LettersMissed = string.Join("", Missed);
                    Feedback = $"Missed letters set to {{{string.Join(", ", Missed)}}}.";
                    return true;
                default:
                    Feedback = $"The given field ({Field}) was not found, game-specific fields are `term`, `lives`, `maxlives`, and `mistakes`";
                    return false;
            }
        }

        /// <summary>
        /// Gives general information about the game and how to play it.
        /// </summary>
        /// <param name="FunConfiguration"></param>
        /// <returns></returns>

        public EmbedBuilder Info(FunConfiguration FunConfiguration) {
            return new EmbedBuilder()
                .WithColor(Color.Magenta)
                .WithTitle("How To Play: Hangman")
                .WithDescription("**Step 1**: Set a TERM! The master can choose a term in DMs with the `game SET TERM [Term]` command.\n" +
                    "**Step 2**: Any player can say `guess` at any moment to see the current status of the guess.\n" +
                    "**Step 3**: Start guessing letters! type `guess [letter]` to guess a letter (you can't guess twice in a row!).\n" +
                    "If you'd like to forego your turn, type `pass`. To check misguessed letters, type `mistakes`.\n" +
                    "Keep guessing until you run out of lives or you complete the word! (Type `lives` to see how many lives you have left)");
        }

        private int GuessChar(char c) {
            c = char.ToUpper(c);
            StringBuilder NewGuess = new StringBuilder(Guess.Length);

            int Result = 0;
            for(int i = 0; i < Math.Min(Term.Length, Guess.Length); i++) {
                if(c == char.ToUpper(Term[i])) {
                    Result++;
                    NewGuess.Append(Term[i]);
                } else {
                    NewGuess.Append(Guess[i]);
                }
            }

            if(Result == 0) {
                RegisterMistake(c);
            }

            Guess = NewGuess.ToString();
            return Result;
        }

        /// <summary>
        /// Handles a message sent by a player in the appropriate channel.
        /// </summary>
        /// <param name="Message">The message context from which the author and content can be obtained.</param>
        /// <param name="GamesDB">The games database in case any player data has to be modified.</param>
        /// <param name="Client">The Discord client used to parse users.</param>
        /// <param name="FunConfiguration">The configuration file containing relevant game information.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task HandleMessage(IMessage Message, GamesDB GamesDB, DiscordSocketClient Client, FunConfiguration FunConfiguration) {

            string Msg = Message.Content;
            if(Msg.ToLower() == "pass") {
                if (Game.LastUserInteracted == Message.Author.Id) {
                    await Message.Channel.SendMessageAsync($"It's not your turn, {Message.Author.Mention}!");
                    return;
                }

                Game.LastUserInteracted = Message.Author.Id;
                await Message.Channel.SendMessageAsync($"{Message.Author.Mention} passed their turn!");
                await Message.DeleteAsync();
                return;
            }

            if(Msg.ToLower() == "mistakes") {
                await Message.Channel.SendMessageAsync($"Mitakes: {MistakesExpression()}");
                return;
            }
            if(Msg.ToLower() == "lives") {
                await Message.Channel.SendMessageAsync($"Lives: {LivesExpression()}");
                return;
            }

            if(Msg.ToLower().StartsWith("guess")) {
                string[] Args = Msg.Split(" ");
                if(Args.Length == 1) {
                    await Message.Channel.SendMessageAsync(DiscordifyGuess());
                    return;
                }
                if (Message.Author.Id == Game.Master) {
                    await Message.Channel.SendMessageAsync("The game master isn't allowed to guess their own word.");
                    return;
                }
                if (Game.LastUserInteracted == Message.Author.Id) {
                    await Message.Channel.SendMessageAsync("You've already guessed! Let someone else play, if it's only you, have the master pass their turn.");
                    return;
                }
                if (!Guess.Contains('_')) {
                    await Message.Channel.SendMessageAsync($"The term was already guessed! You're late to the party. Waiting for the Game Master to change it.");
                    return;
                }
                string newGuess = string.Join(' ', Args[1..]);
                if (newGuess.Length == 1) {
                    char c = newGuess[0];
                    if (!char.IsLetter(c)) {
                        await Message.Channel.SendMessageAsync($"Character {c} isn't a valid letter!");
                        return;
                    }
                    if (LettersMissed.Contains(char.ToUpper(c)) || Guess.ToUpper().Contains(char.ToUpper(c))) {
                        await Message.Channel.SendMessageAsync($"The letter {c} has already been guessed!");
                        return;
                    }

                    Game.LastUserInteracted = Message.Author.Id;
                    int Count = GuessChar(c);
                    if (Count > 0) {
                        await Message.Channel.SendMessageAsync($"{Message.Author.Mention} guessed letter {char.ToUpper(c)}!\n" +
                            $"**The term has {Count} {char.ToUpper(c)}{(Count > 1 ? "s" : "")}!** {DiscordifyGuess()}");
                        await Message.DeleteAsync();

                        if(!Guess.Contains('_')) {
                            await new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithTitle("Victory!")
                                .WithDescription($"The term was {Term}! \nThe master can now choose a new term or offer their position to another player with the `game set master [Player]` command.")
                                .SendEmbed(Message.Channel);
                            return;
                        }
                    } else {
                        await Message.Channel.SendMessageAsync($"{Message.Author.Mention} guessed letter {char.ToUpper(c)}!\n" +
                            $"Whoops! Wrong guess. {LivesExpression()}");
                        await Message.DeleteAsync();
                        if(Lives == 0) {
                            await new EmbedBuilder()
                                .WithColor(Color.Red)
                                .WithTitle("Defeat!")
                                .WithDescription($"The term was {Term}! \nThe master can now choose a new term or offer their position to another player with the `game set master [Player]` command.")
                                .SendEmbed(Message.Channel);
                        }
                    }
                    GamesDB.SaveChanges();
                    return;
                }
                if (newGuess.Length > 1) {
                    if (newGuess.ToLower() != Term.ToLower()) {
                        await Message.Channel.SendMessageAsync($"{Message.Author.Mention} guessed the term to be \"{newGuess}\"!\n" +
                            $"It seems as though that isn't the word, though!");
                        Game.LastUserInteracted = Message.Author.Id;
                        return;
                    }

                    Guess = Term;
                    await new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle("Correct!")
                        .WithDescription($"The term was {Term}! \nThe master can now choose a new term or offer their position to another player with the `game set master [Player]` command.")
                        .SendEmbed(Message.Channel);
                    return;
                }
            }
        }
    }
}
