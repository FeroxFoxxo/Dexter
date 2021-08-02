using Dexter.Configurations;
using Dexter.Databases.Games;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Helpers.Games
{

    /// <summary>
    /// Represents an instance of a hangman game.
    /// </summary>

    public class GameHangman : IGameTemplate
    {

        /// <summary>
        /// The game instance that this specific game is attached to.
        /// </summary>

        public GameInstance game;

        /// <summary>
        /// Creates a new instance of a hangman game based on a generic game instance.
        /// </summary>
        /// <param name="game">The generic game instance to inherit from.</param>

        public GameHangman(GameInstance game)
        {
            this.game = game;
            if (string.IsNullOrWhiteSpace(game.Data)) game.Data = EmptyData;
        }

        //Data structure: "term, guess, lives, maxlives, lettersmissed";
        const string EmptyData = "Default, _______, 6, 6, ";

        private string Term
        {
            get
            {
                return game.Data.Split(", ")[0].Replace(GameInstance.CommaRepresentation, ",");
            }
            set
            {
                string processedValue = value.Replace(",", GameInstance.CommaRepresentation);
                string[] newValue = game.Data.Split(", ");
                newValue[0] = processedValue;
                game.Data = string.Join(", ", newValue);
            }
        }

        private string Guess
        {
            get
            {
                return game.Data.Split(", ")[1].Replace(GameInstance.CommaRepresentation, ",");
            }
            set
            {
                string processedValue = value.Replace(",", GameInstance.CommaRepresentation);
                string[] newValue = game.Data.Split(", ");
                newValue[1] = processedValue;
                game.Data = string.Join(", ", newValue);
            }
        }

        private int Lives
        {
            get
            {
                return int.Parse(game.Data.Split(", ")[2]);
            }
            set
            {
                if (value < 0) return;
                if (value > MaxLives) MaxLives = value;
                string processedValue = value.ToString();
                string[] newValue = game.Data.Split(", ");
                newValue[2] = processedValue;
                game.Data = string.Join(", ", newValue);
            }
        }

        private int MaxLives
        {
            get
            {
                return int.Parse(game.Data.Split(", ")[3]);
            }
            set
            {
                if (value < 1) return;
                string processedValue = value.ToString();
                string[] newValue = game.Data.Split(", ");
                newValue[3] = processedValue;
                game.Data = string.Join(", ", newValue);
                if (Lives > value) Lives = value;
            }
        }

        private string LettersMissed
        {
            get
            {
                string s = game.Data.Split(", ")[4];
                if (s == "-") return "";
                return s;
            }
            set
            {
                string[] NewValue = game.Data.Split(", ");
                NewValue[4] = value.Length == 0 ? "-" : value;
                game.Data = string.Join(", ", NewValue);
            }
        }

        /// <summary>
        /// Represents the general status and data of a Hangman Game.
        /// </summary>
        /// <param name="Client">SocketClient used to parse UserIDs.</param>
        /// <returns>An Embed detailing the various aspects of the game in its current instance.</returns>

        public EmbedBuilder GetStatus(DiscordSocketClient Client)
        {
            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle($"{game.Title} (Game {game.GameID})")
                .WithDescription($"{game.Description}\n**Term**: {DiscordifyGuess()}")
                .AddField("Lives", LivesExpression(), true)
                .AddField("Wrong Guesses", MistakesExpression(), true)
                .AddField("Available Guesses", MissingLettersExpression(), true)
                .AddField("Master", Client.GetUser(game.Master)?.GetUserInformation() ?? "<N/A>")
                .AddField(game.Banned.Length > 0, "Banned Players", game.BannedMentions.TruncateTo(500));
        }

        private void RegisterMistake(char c)
        {
            Lives--;
            LettersMissed += c;
        }

        private string DiscordifyGuess()
        {
            char[] treated = Guess.Replace(" ", "/")
                .ToCharArray();
            return string.Join(" ", treated).Replace("_", "\\_");
        }

        private static string ObscureTerm(string term)
        {
            char[] chars = term.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsLetter(chars[i])) chars[i] = '_';
            }
            return string.Join("", chars);
        }

        const char LifeFullChar = '♥';
        const char LifeEmptyChar = '☠';
        private string LivesExpression()
        {
            char[] expression = new char[MaxLives];
            for (int i = 0; i < MaxLives; i++)
            {
                expression[i] = i < Lives ? LifeFullChar : LifeEmptyChar;
            }
            return string.Join("", expression);
        }

        private string MistakesExpression()
        {
            string missed = string.Join(", ", LettersMissed);
            if (missed.Length == 0) return "-";
            return missed.ToString();
        }

        private string MissingLettersExpression()
        {
            HashSet<char> missing = new HashSet<char>();
            foreach (char c in ScorePerLetter.Keys)
            {
                missing.Add(c);
            }

            foreach (char c in LettersMissed.ToCharArray())
            {
                if (missing.Contains(c)) missing.Remove(c);
            }
            foreach (char c in Guess.ToUpper().ToCharArray())
            {
                if (missing.Contains(c)) missing.Remove(c);
            }

            return string.Join("", missing);
        }

        /// <summary>
        /// Resets the game state to its initial default value.
        /// </summary>
        /// <param name="FunConfiguration">Settings related to the fun module, which contain the default lives parameter.</param>
        /// <param name="GamesDB">The database containing player information, set to <see langword="null"/> to avoid resetting scores.</param>

        public void Reset(FunConfiguration FunConfiguration, GamesDB GamesDB)
        {
            game.Data = EmptyData;
            game.LastUserInteracted = game.Master;
            Lives = MaxLives = FunConfiguration.HangmanDefaultLives;
            if (GamesDB is null) return;
            Player[] Players = GamesDB.GetPlayersFromInstance(game.GameID);
            foreach (Player p in Players)
            {
                p.Score = 0;
                p.Lives = 0;
            }
        }

        const int MAX_LIVES_ALLOWED = 20;

        /// <summary>
        /// Sets a local <paramref name="field"/> to a given <paramref name="value"/>.
        /// </summary>
        /// <remarks>Valid <paramref name="field"/> values are: TERM, LIVES, MAXLIVES, and MISTAKES.</remarks>
        /// <param name="field">The name of the field to modify.</param>
        /// <param name="value">The value to set the field to.</param>
        /// <param name="funConfiguration">The Fun Configuration settings file, which holds relevant data such as default lives.</param>
        /// <param name="feedback">In case this operation wasn't possible, its reason, or useful feedback even if the operation was successful.</param>
        /// <returns><see langword="true"/> if the operation was successful, otherwise <see langword="false"/>.</returns>

        public bool Set(string field, string value, FunConfiguration funConfiguration, out string feedback)
        {
            feedback = "";
            int n;

            switch (field.ToLower())
            {
                case "word":
                case "term":
                    if (value.Contains('_'))
                    {
                        feedback = "The term cannot contain underscores!";
                        return false;
                    }
                    else if (value.Contains('@'))
                    {
                        feedback = "The term cannot contain the at symbol (@)!";
                        return false;
                    }
                    else if (value.Length > 256)
                    {
                        feedback = "The term is too long!";
                        return false;
                    }
                    Reset(funConfiguration, null);
                    Term = value;
                    Guess = ObscureTerm(value);
                    feedback = $"Success! Term = {value}, Guess = \"{DiscordifyGuess()}\"";
                    return true;
                case "lives":
                    if (!int.TryParse(value, out n))
                    {
                        feedback = $"Unable to parse {value} into an integer value.";
                        return false;
                    }
                    if (n > MAX_LIVES_ALLOWED)
                    {
                        feedback = $"Too many lives! Please keep it below {MAX_LIVES_ALLOWED}";
                        return false;
                    }
                    Lives = n;
                    feedback = $"Lives set to {Lives}/{MaxLives}";
                    return true;
                case "maxlives":
                    if (!int.TryParse(value, out n))
                    {
                        feedback = $"Unable to parse {value} into an integer value.";
                        return false;
                    }
                    if (n > MAX_LIVES_ALLOWED)
                    {
                        feedback = $"Too many lives! Please keep it below {MAX_LIVES_ALLOWED}";
                        return false;
                    }
                    MaxLives = n;
                    feedback = $"Lives set to {Lives}/{MaxLives}";
                    return true;
                case "missed":
                case "mistakes":
                    if (value.ToLower() == "none")
                    {
                        LettersMissed = "";
                        feedback = "Reset mistakes.";
                        return true;
                    }

                    HashSet<char> missed = new HashSet<char>();
                    foreach (char c in value.ToCharArray())
                    {
                        if (!char.IsLetter(c)) missed.Add(char.ToUpper('c'));
                    }
                    LettersMissed = string.Join("", missed);
                    feedback = $"Missed letters set to {{{string.Join(", ", missed)}}}.";
                    return true;
                default:
                    feedback = $"The given field ({field}) was not found, game-specific fields are `term`, `lives`, `maxlives`, and `mistakes`";
                    return false;
            }
        }

        /// <summary>
        /// Gives general information about the game and how to play it.
        /// </summary>
        /// <param name="funConfiguration"></param>
        /// <returns></returns>

        public EmbedBuilder Info(FunConfiguration funConfiguration)
        {
            return new EmbedBuilder()
                .WithColor(Color.Magenta)
                .WithTitle("How To Play: Hangman")
                .WithDescription("**Step 1**: Set a TERM! The master can choose a term in DMs with the `game SET TERM [Term]` command.\n" +
                    "**Step 2**: Any player can say `guess` at any moment to see the current status of the guess.\n" +
                    "**Step 3**: Start guessing letters! type `guess [letter]` to guess a letter (you can't guess twice in a row!).\n" +
                    "Feeling brave? Guess the whole term with `guess [term]`, it doesn't cost lives, but it does cost a turn.\n" +
                    "If you'd like to forego your turn, type `pass`. To check misguessed letters, type `mistakes`.\n" +
                    "Keep guessing until you run out of lives or you complete the word! (Type `lives` to see how many lives you have left)");
        }

        private int GuessChar(char c)
        {
            c = char.ToUpper(c);
            StringBuilder newGuess = new StringBuilder(Guess.Length);

            int result = 0;
            for (int i = 0; i < Math.Min(Term.Length, Guess.Length); i++)
            {
                if (c == char.ToUpper(Term[i]))
                {
                    result++;
                    newGuess.Append(Term[i]);
                }
                else
                {
                    newGuess.Append(Guess[i]);
                }
            }

            if (result == 0)
            {
                RegisterMistake(c);
            }

            Guess = newGuess.ToString();
            return result;
        }

        /// <summary>
        /// Handles a message sent by a player in the appropriate channel.
        /// </summary>
        /// <param name="message">The message context from which the author and content can be obtained.</param>
        /// <param name="gamesDB">The games database in case any player data has to be modified.</param>
        /// <param name="client">The Discord client used to parse users.</param>
        /// <param name="funConfiguration">The configuration file containing relevant game information.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task HandleMessage(IMessage message, GamesDB gamesDB, DiscordSocketClient client, FunConfiguration funConfiguration)
        {
            if (message.Channel is IDMChannel) return;
            Player player = gamesDB.GetOrCreatePlayer(message.Author.Id);

            string msg = message.Content.Replace("@", "@-");
            if (msg.ToLower() == "pass")
            {
                if (game.LastUserInteracted == message.Author.Id)
                {
                    await message.Channel.SendMessageAsync($"It's not your turn, {message.Author.Mention}!");
                    return;
                }

                game.LastUserInteracted = message.Author.Id;
                await message.Channel.SendMessageAsync($"{message.Author.Mention} passed their turn!");
                await message.DeleteAsync();
                return;
            }

            if (msg.ToLower() == "mistakes")
            {
                await message.Channel.SendMessageAsync($"Mistakes: {MistakesExpression()}");
                return;
            }
            if (msg.ToLower() == "lives")
            {
                await message.Channel.SendMessageAsync($"Lives: {LivesExpression()}");
                return;
            }

            if (msg.ToLower().StartsWith("guess"))
            {
                string[] args = msg.Split(" ");
                if (args.Length == 1)
                {
                    await message.Channel.SendMessageAsync(DiscordifyGuess());
                    return;
                }
                if (message.Author.Id == game.Master)
                {
                    await message.Channel.SendMessageAsync("The game master isn't allowed to guess their own word.");
                    return;
                }
                if (game.LastUserInteracted == message.Author.Id)
                {
                    await message.Channel.SendMessageAsync("You've already guessed! Let someone else play, if it's only you, have the master pass their turn.");
                    return;
                }
                if (!Guess.Contains('_'))
                {
                    await message.Channel.SendMessageAsync($"The term was already guessed! You're late to the party. Waiting for the Game Master to change it.");
                    return;
                }
                if (Lives < 1)
                {
                    await message.Channel.SendMessageAsync($"You're out of lives! Choose a new term before continuing.");
                    return;
                }
                string newGuess = string.Join(' ', args[1..]);
                if (newGuess.Length == 1)
                {
                    char c = newGuess[0];
                    if (!char.IsLetter(c))
                    {
                        await message.Channel.SendMessageAsync($"Character {c} isn't a valid letter!");
                        return;
                    }
                    if (LettersMissed.Contains(char.ToUpper(c)) || Guess.ToUpper().Contains(char.ToUpper(c)))
                    {
                        await message.Channel.SendMessageAsync($"The letter {c} has already been guessed!");
                        return;
                    }

                    game.LastUserInteracted = message.Author.Id;
                    int count = GuessChar(c);
                    if (count > 0)
                    {
                        int score = 0;
                        string scoreExpression = "";
                        if (ScorePerLetter.ContainsKey(char.ToUpper(c)))
                        {
                            score = count * ScorePerLetter[char.ToUpper(c)];
                            scoreExpression = $" (+{score}P)";
                            player.Score += score;
                        }

                        await message.Channel.SendMessageAsync($"{message.Author.Mention} guessed letter {char.ToUpper(c)}! **The term has {count} {char.ToUpper(c)}{(count > 1 ? "s" : "")}{scoreExpression}!**" +
                            $"\n{DiscordifyGuess()}");
                        await message.DeleteAsync();

                        if (!Guess.Contains('_'))
                        {
                            await new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithTitle("Victory!")
                                .WithDescription($"The term was {Term}! \nThe master can now choose a new term or offer their position to another player with the `game set master [Player]` command.")
                                .SendEmbed(message.Channel);
                            return;
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"{message.Author.Mention} guessed letter {char.ToUpper(c)}!\n" +
                            $"Whoops! Wrong guess. {LivesExpression()}");
                        await message.DeleteAsync();
                        if (Lives == 0)
                        {
                            await new EmbedBuilder()
                                .WithColor(Color.Red)
                                .WithTitle("Defeat!")
                                .WithDescription($"The term was {Term}! \nThe master can now choose a new term or offer their position to another player with the `game set master [Player]` command.")
                                .SendEmbed(message.Channel);
                        }
                    }
                    gamesDB.SaveChanges();
                    return;
                }
                if (newGuess.Length > 1)
                {
                    game.LastUserInteracted = message.Author.Id;
                    if (!ConsiderEqual(newGuess, Term))
                    {
                        await message.Channel.SendMessageAsync($"{message.Author.Mention} guessed the term to be \"{newGuess}\"!\n" +
                            $"It seems as though that isn't the word, though!");
                        return;
                    }

                    int score = ValueDifference(Guess, Term);
                    player.Score += score;
                    Guess = Term;
                    await new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle($"Correct! (+{score} Point{(score > 1 ? "s" : "")})")
                        .WithDescription($"The term was {Term}! \nThe master can now choose a new term or offer their position to another player with the `game set master [Player]` command.\n")
                        .SendEmbed(message.Channel);
                    return;
                }
            }
        }

        private static int ValueDifference(string current, string target)
        {
            int result = 0;

            for (int i = 0; i < Math.Min(current.Length, target.Length); i++)
            {
                if (current[i] == '_' && ScorePerLetter.ContainsKey(char.ToUpper(target[i])))
                {
                    result += ScorePerLetter[char.ToUpper(target[i])];
                }
            }

            return result;
        }

        private static bool ConsiderEqual(string a, string b)
        {
            a = a.ToLower();
            b = b.ToLower();
            StringBuilder aStr = new StringBuilder();
            StringBuilder bStr = new StringBuilder();
            foreach (char c in a.ToCharArray())
            {
                if (char.IsLetter(c)) aStr.Append(c);
            }
            foreach (char c in b.ToCharArray())
            {
                if (char.IsLetter(c)) bStr.Append(c);
            }
            return aStr.ToString() == bStr.ToString();
        }

        private static readonly Dictionary<char, int> ScorePerLetter = new Dictionary<char, int>() {
            {'A', 1},
            {'B', 3},
            {'C', 3},
            {'D', 2},
            {'E', 1},
            {'F', 4},
            {'G', 2},
            {'H', 4},
            {'I', 1},
            {'J', 8},
            {'K', 5},
            {'L', 1},
            {'M', 3},
            {'N', 1},
            {'O', 1},
            {'P', 3},
            {'Q', 10},
            {'R', 1},
            {'S', 1},
            {'T', 1},
            {'U', 1},
            {'V', 4},
            {'W', 4},
            {'X', 8},
            {'Y', 4},
            {'Z', 10}
        };
    }
}
