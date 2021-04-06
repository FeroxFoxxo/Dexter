using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;

namespace Dexter.Helpers.Games {
    class GameMinesweeper : IGameTemplate {

        const string EmptyData = "???/???/???, XXX/X8X/XXX, 0, 3, 3, 8";
        const int MaxWidth = 26;
        const int MaxHeight = 18;
        const int MinWidth = 3;
        const int MinHeight = 3;
        const float MaxMineRatio = 0.4f;
        const char ZWSP = '​';

        private int MaxMines {
            get {
                return (int) (Width * Height * MaxMineRatio);
            }
        }

        private GameInstance game;

        public GameMinesweeper(GameInstance game) {
            this.game = game; 
        }

        private string StateRaw {
            get {
                return game.Data.Split(", ")[0];
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[0] = value;
                game.Data = string.Join(", ", newValue);
            }
        }

        private string BoardRaw {
            get {
                return game.Data.Split(", ")[1];
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[1] = value;
                game.Data = string.Join(", ", newValue);
            }
        }

        private ulong BoardID {
            get {
                return ulong.Parse(game.Data.Split(", ")[2]);
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[2] = value.ToString();
                game.Data = string.Join(", ", newValue);
            }
        }

        private int Height {
            get {
                return int.Parse(game.Data.Split(", ")[3]);
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[3] = value.ToString();
                game.Data = string.Join(", ", newValue);
            }
        }

        private int Width {
            get {
                return int.Parse(game.Data.Split(", ")[4]);
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[4] = value.ToString();
                game.Data = string.Join(", ", newValue);
            }
        }

        private int Mines {
            get {
                return int.Parse(game.Data.Split(", ")[5]);
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[5] = value.ToString();
                game.Data = string.Join(", ", newValue);
            }
        }

        private char[,] State {
            get {
                char[,] result = new char[Height, Width];
                string[] raw = StateRaw.Split('/');
                for (int i = 0; i < result.GetLength(0); i++) {
                    for (int j = 0; j < result.GetLength(1); j++)
                        result[i, j] = raw[i][j];
                }
                return result;
            }
            set {
                StringBuilder builder = new StringBuilder();
                int h = value.GetLength(0);
                int w = value.GetLength(1);
                for (int i = 0; i < h; i++) {
                    for (int j = 0; j < w; j++)
                        builder.Append(value[i, j]);
                    if (i != h - 1) builder.Append('/');
                }
                StateRaw = builder.ToString();
            }
        }

        private char[,] Board {
            get {
                char[,] result = new char[Height, Width];
                string[] raw = BoardRaw.Split('/');
                for (int i = 0; i < result.GetLength(0); i++) {
                    for (int j = 0; j < result.GetLength(1); j++)
                        result[i, j] = raw[i][j];
                }
                return result;
            }
            set {
                StringBuilder builder = new StringBuilder();
                int h = value.GetLength(0);
                int w = value.GetLength(1);
                for (int i = 0; i < h; i++) {
                    for (int j = 0; j < w; j++)
                        builder.Append(value[i, j]);
                    if (i != h - 1) builder.Append('/');
                }
                BoardRaw = builder.ToString();
            }
        }

        private char[,] GenerateBoard(int height, int width, int mineCount, Random rnd) {
            
            char[,] board = new char[height, width];

            int[,] dangers = new int[height, width];
            int[] mines = new int[height * width];
            for (int i = 0; i < height * width; i++)
                mines[i] = i;
            mines = mines.OrderBy(x => rnd.Next()).ToArray()[..mineCount];

            foreach (int i in mines) {
                int x = i % width;
                int y = i / width;
                for (int dx = -1; dx <= 1; dx++) {
                    if (x + dx < 0 || x + dx >= width) continue;
                    for (int dy = -1; dy <= 1; dy++) {
                        if (y + dy < 0 || y + dy >= height) continue;
                        dangers[y + dy, x + dx]++;
                    }
                }
                board[y, x] = 'X';
            }

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++) {
                    if (board[y, x] != 'X') board[y, x] = dangers[y, x].ToString()[0]; 
                }

            return board;
        }

        private char[,] GenerateNewState(int height, int width) {
            char[,] state = new char[height, width];

            for (int i = 0; i < state.GetLength(0); i++) {
                for (int j = 0; j < state.GetLength(1); j++) {
                    state[i, j] = '?';
                }
            }

            return state;
        }

        private bool CheckWin(char[,] state, char[,] board) {
            for (int i = 0; i < state.GetLength(0); i++) {
                for (int j = 0; j < state.GetLength(1); j++) {
                    if (state[i, j] is '?' or 'F' && board[i, j] != 'X') return false;
                }
            }
            return true;
        }

        private bool ProbeCell(int x, int y) {
            if (State[y, x] != '?') return false;

            char[,] newState = State;
            char[,] board = Board;

            newState[y, x] = board[y, x];

            switch (newState[y, x]) {
                case 'X':
                    for (int i = 0; i < newState.GetLength(0); i++) {
                        for (int j = 0; j < newState.GetLength(1); j++) {
                            if (board[i, j] == 'X') newState[i, j] = 'X';
                        }
                    }
                    break;
                case '0':
                    for (int dx = -1; dx <= 1; dx++) {
                        if (x + dx < 0 || x + dx >= newState.GetLength(1)) continue;
                        for (int dy = -1; dy <= 1; dy++) {
                            if (y + dy < 0 || y + dy >= newState.GetLength(0)) continue;
                            ProbeCellRecursive(ref newState, board, x + dx, y + dy);
                        }
                    }
                    break;
            }
            State = newState;
            return true;
        }

        private void ProbeCellRecursive(ref char[,] state, char[,] board, int x, int y) {
            if (state[y, x] != '?') return;

            state[y, x] = board[y, x];

            if (board[y, x] == '0') {
                for (int dx = -1; dx <= 1; dx++) {
                    for (int dy = -1; dy <= 1; dy++) {
                        if (x + dx < 0 || x + dx >= state.GetLength(1)) continue;
                        if (y + dy < 0 || y + dy >= state.GetLength(0)) continue;
                        ProbeCellRecursive(ref state, board, x + dx, y + dy);
                    }
                }
            }
        }

        private int CountMatrix(char[,] matrix, char c) {
            int count = 0;
            for (int i = 0; i < matrix.GetLength(0); i++) {
                for (int j = 0; j < matrix.GetLength(1); j++) {
                    if (matrix[i, j] == c) count++;
                }
            }
            return count;
        }

        private string RenderMatrix(char[,] matrix) {
            StringBuilder builder = new StringBuilder();
            builder.Append(LetterLabel(matrix.GetLength(1)) + '\n');

            for (int i = 0; i < matrix.GetLength(0); i++) {
                builder.Append($"`{matrix.GetLength(0) - i:D2}`");
                for (int j = 0; j < matrix.GetLength(1); j++) {
                    builder.Append(ToEmoji(matrix[i, j]));
                }
                builder.Append($"`{matrix.GetLength(0) - i:D2}`\n");
            }

            builder.Append(LetterLabel(matrix.GetLength(1)));
            return builder.ToString();
        }

        const int cellSize = 16;

        private Bitmap RenderMatrixImage(char[,] matrix) {
            Bitmap result = new Bitmap(cellSize * (matrix.GetLength(1) + 2), cellSize * (matrix.GetLength(0) + 2));
            
            Dictionary<char, System.Drawing.Image> cellImages = new Dictionary<char, System.Drawing.Image>();
            foreach (KeyValuePair<char, string> kvp in CellImageNames) {
                cellImages.Add(kvp.Key, System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, $"{kvp.Value}.png")));
            }

            using (Graphics g = Graphics.FromImage(result)) {
                for (int x = 1; x < matrix.GetLength(1) + 1; x++) {
                    for (int y = 1; y < matrix.GetLength(0) + 1; y++) {
                        g.DrawImage(cellImages[matrix[y - 1, x - 1]], x * cellSize, y * cellSize, cellSize, cellSize);
                    }
                }

                using (System.Drawing.Image corner = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, $"{Corner}.png"))) {
                    for (int x = 0; x < 2; x++)
                        for (int y = 0; y < 2; y++)
                            g.DrawImage(corner, x * (result.Width - cellSize), y * (result.Height - cellSize), cellSize, cellSize);
                }

                System.Drawing.Image[] letterLabels = GetLabels(matrix.GetLength(1), false);
                for (int y = 0; y < 2; y++) {
                    int ry = y * (result.Height - cellSize);
                    using (System.Drawing.Image label = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, $"{LetterLabels[y]}.png"))) {
                        for (int x = cellSize; x < result.Width - cellSize; x += cellSize) {
                            g.DrawImage(label, x, ry, cellSize, cellSize);
                        }
                    }

                    for (int x = 1; x < letterLabels.Length + 1; x++) {
                        g.DrawImage(letterLabels[x - 1], x * cellSize, ry, cellSize, cellSize);
                    }
                }

                System.Drawing.Image[] numberLabels = GetLabels(matrix.GetLength(0), true);
                for (int x = 0; x < 2; x++) {
                    int rx = x * (result.Width - cellSize);
                    using (System.Drawing.Image label = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, $"{NumLabels[x]}.png"))) {
                        for (int y = cellSize; y < result.Height - cellSize; y += cellSize) {
                            g.DrawImage(label, rx, y, cellSize, cellSize);
                        }
                    }

                    for (int y = 1; y < numberLabels.Length + 1; y++) {
                        g.DrawImage(numberLabels[y - 1], rx, y * cellSize, cellSize, cellSize);
                    }
                }
            }

            return result;
        }

        private const string MinesweeperPath = "Images/Games/Minesweeper";
        private const string LabelsDirectory = "Labels";
        private readonly string Corner = "GridCorner";
        private readonly string[] NumLabels = new string[] {"NumberLabelLeft", "NumberLabelRight"};
        private readonly string[] LetterLabels = new string[] {"LetterLabelTop", "LetterLabelBottom"};
        private readonly Dictionary<char, string> CellImageNames = new Dictionary<char, string>() {
            {'0', "Cell0"},
            {'1', "Cell1"},
            {'2', "Cell2"},
            {'3', "Cell3"},
            {'4', "Cell4"},
            {'5', "Cell5"},
            {'6', "Cell6"},
            {'7', "Cell7"},
            {'8', "Cell8"},
            {'?', "Cell"},
            {'X', "Mine"},
            {'F', "CellFlag"}
        };

        private System.Drawing.Image[] GetLabels(int length, bool isNumber) {
            System.Drawing.Image[] result = new System.Drawing.Image[length];

            for (int i = 0; i < length; i++) {
                if (isNumber) result[i] = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, LabelsDirectory, $"{i + 1}.png"));
                else result[i] = System.Drawing.Image.FromFile(Path.Join(MinesweeperPath, LabelsDirectory, $"{(char) ('A' + i)}.png"));
            }

            return result;
        }

        private string LetterLabel(int length) {
            StringBuilder builder = new StringBuilder();
            builder.Append("`  `");

            for (int i = 0; i < length; i++) {
                builder.Append($"{Indicators[i]}{ZWSP}");
            }

            return builder.ToString();
        }

        private Dictionary<int, string> Indicators = new Dictionary<int, string>() {
            {0, "🇦"},
            {1, "🇧"},
            {2, "🇨"},
            {3, "🇩"},
            {4, "🇪"},
            {5, "🇫"},
            {6, "🇬"},
            {7, "🇭"},
            {8, "🇮"},
            {9, "🇯"},
            {10, "🇰"},
            {11, "🇱"},
            {12, "🇲"},
            {13, "🇳"},
            {14, "🇴"},
            {15, "🇵"},
            {16, "🇶"},
            {17, "🇷"},
            {18, "🇸"},
            {19, "🇹"},
            {20, "🇺"},
            {21, "🇻"},
            {22, "🇼"},
            {23, "🇽"},
            {24, "🇾"},
            {25, "🇿"}
        };

        public EmbedBuilder GetStatus(DiscordSocketClient client) {
            return new EmbedBuilder()
                .WithColor(Discord.Color.Blue)
                .WithTitle($"{game.Title} (Game {game.GameID})")
                .WithDescription($"{game.Description}")
                .AddField("Dimensions", $"{Width}×{Height}", true)
                .AddField("Mines", $"{Mines}{CharMine}", true)
                .AddField("Master", client.GetUser(game.Master).GetUserInformation())
                .AddField(game.Banned.Length > 0, "Banned Players", game.BannedMentions.TruncateTo(500));
        }

        public void Reset(FunConfiguration funConfiguration, GamesDB gamesDB) {
            game.Data = EmptyData;
            game.LastUserInteracted = game.Master;
            if (gamesDB is null) return;
            Player[] players = gamesDB.GetPlayersFromInstance(game.GameID);
            foreach (Player p in players) {
                p.Score = 0;
                p.Lives = 0;
            }
        }

        public bool Set(string field, string value, FunConfiguration funConfiguration, out string feedback) {
            if (!int.TryParse(value, out int number)) {
                feedback = $"\nAll fields for this game are numeric, unable to parse \"{value}\" into an integer value.\n" +
                    $"Did you mean to use a default field instead?";
                return false;
            }

            int mines = Mines;
            
            switch(field.ToLower()) {
                case "width":
                    if (number > MaxWidth || number < MinWidth) {
                        feedback = $"Invalid width! The width must be between {MinWidth} and {MaxWidth}.";
                        return false;
                    }
                    Width = number;
                    if (mines > MaxMines) mines = MaxMines;
                    Board = GenerateBoard(Height, Width, mines, new Random());
                    State = GenerateNewState(Height, Width);
                    feedback = $"Set \"width\" to {number} and regenerated game board [{Width}x{Height}]. Maximum mine count for this size is {MaxMines}.";
                    return true;
                case "height":
                    if (number > MaxHeight || number < MinHeight) {
                        feedback = $"Invalid height! The height must be between {MinHeight} and {MaxHeight}.";
                        return false;
                    }
                    Height = number;
                    if (mines > MaxMines) mines = MaxMines;
                    Board = GenerateBoard(Height, Width, mines, new Random());
                    State = GenerateNewState(Height, Width);
                    feedback = $"Set \"height\" to {number} and regenerated game board [{Width}x{Height}]. Maximum mine count for this size is {MaxMines}.";
                    return true;
                case "mines":
                    if (number < 0) {
                        feedback = $"Invalid value! Number of mines can't be a negative number.";
                        return false;
                    }
                    feedback = $"Set \"mines\" to {number} and regenerated game board [{Width}x{Height}]. Maximum mine count for this size is {MaxMines}.";
                    Mines = mines = number;
                    if (mines > MaxMines) mines = MaxMines;
                    Board = GenerateBoard(Height, Width, mines, new Random());
                    State = GenerateNewState(Height, Width);
                    return true;
            }

            feedback = $"Invalid field: \"{field}\" is not a default field nor \"width\", \"height\", or \"mines\".";
            return false;
        }

        public EmbedBuilder Info(FunConfiguration funConfiguration) {
            return new EmbedBuilder()
                .WithColor(Discord.Color.Magenta)
                .WithTitle("How to Play: Minesweeper")
                .WithDescription("**Step 1**: Set your desired parameters for the board using the `~game set [field] [value]` command. These fields are `width`, `height` and `mines`.\n" +
                    "Here are some defaults, if you'd like to try them:\n" +
                    "Beginner: [10x10] (10💣)\n" +
                    "Intermediate: [16x16] (40💣)\n" +
                    "Advanced: [26x18] (99💣)\n" +
                    "**Step 2**: Create the board view by typing `board` into the game chat.\n" +
                    "**Step 3**: Begin probing cells! Type the name of a cell (such as `C3` or `G12`) to probe it. Or type `flag [Cell]` to toggle a flag in a cell.)\n" +
                    "Since this game is singleplayer, only the game master can interact with it. The number on each cell tells you how many mines are on its immediate surroundings, anywhere from 0 to 8. If you probe a mine, it's game over!\n" +
                    "You win once there are no more cells to probe that do not contain a mine.");
        }

        public string ToEmoji(char c) {
            if (c == 'F') return CharFlag;
            if (c == 'X') return CharMine;
            if (c == '?') return CharUnknown;
            if (int.TryParse(c.ToString(), out int v)) {
                return ProbeChars[v]; 
            }
            return "";
        }

        const string CharFlag = "🚩";
        const string CharMine = "💣";
        const string CharUnknown = "🔲";
        private readonly Dictionary<int, string> ProbeChars = new Dictionary<int, string>() {
            {0, "🟦"},
            {1, "1️⃣"},
            {2, "2️⃣"},
            {3, "3️⃣"},
            {4, "4️⃣"},
            {5, "5️⃣"},
            {6, "6️⃣"},
            {7, "7️⃣"},
            {8, "8️⃣"}
        };

        private bool TryParsePos(string input, out Tuple<int, int> result) {
            result = new Tuple<int, int>(0, 0);
            if (input.Length < 2) return false;
            input = input.ToUpper();

            if (input[0] < 'A' || input[0] > 'Z') return false;
            int x = input[0] - 'A';

            if (!int.TryParse(input[1..], out int y)) return false;

            result = new Tuple<int, int>(x, --y);
            return true;
        }

        private Tuple<int, int> ToRealPos(Tuple<int, int> input) {
            int y = input.Item2;
            if (input.Item2 >= Height) y = Height - 1;
            if (input.Item2 < 0) y = 0;

            return new Tuple<int, int>(input.Item1, y);
        }

        public async Task HandleMessage(IMessage message, GamesDB gamesDB, DiscordSocketClient client, FunConfiguration funConfiguration) {
            if (message.Author.Id != game.Master) return;

            string msg = message.Content.ToLower();
            string[] args = msg.Split(' ');

            IUserMessage board = null;
            if (BoardID != 0) board = await message.Channel.GetMessageAsync(BoardID) as IUserMessage;

            /*if (msg == "board") {
                if (board is not null) await board.DeleteAsync();
                IUserMessage newBoard = await message.Channel.SendMessageAsync(RenderMatrix(State));
                BoardID = newBoard.Id;
                return;
            }*/
            bool toRender = false;
            if (msg is "board" or "render") {
                toRender = true;
            }

            if (msg == "solve") {
                if (board is not null) await board.DeleteAsync();
                string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
                string filepath = Path.Join(imageChacheDir, $"Minesweeper{game.Master}_SOLUTION.png");
                System.Drawing.Image image = RenderMatrixImage(Board);
                image.Save(filepath);
                IUserMessage newBoard = await message.Channel.SendFileAsync(filepath);
                BoardID = newBoard.Id;
                await message.DeleteAsync();
            }

            if (msg == "reset") {
                BoardID = 0;
                Board = GenerateBoard(Height, Width, Mines < MaxMines ? Mines : MaxMines, new Random());
                State = GenerateNewState(Height, Width);
                await message.Channel.SendMessageAsync($"Reset your {Width} by {Height} board!");
            }

            if (msg.StartsWith("flag") || msg.StartsWith("unflag")) {
                if (args.Length < 2) {
                    await message.Channel.SendMessageAsync("You must provide a position to flag or unflag!");
                    return;
                }
                if (!TryParsePos(args[1], out Tuple<int, int> flagpos)) {
                    await message.Channel.SendMessageAsync($"Unable to resolve position {args[1]}, please type a letter followed by a number, no spaces!");
                    return;
                }
                flagpos = ToRealPos(flagpos);
                if (State[flagpos.Item2, flagpos.Item1] is not '?' or 'F') {
                    await message.Channel.SendMessageAsync($"Unable to toggle flag at this location, it has already been probed!");
                    return;
                }

                char[,] modifiedState = State;
                modifiedState[flagpos.Item2, flagpos.Item1] = modifiedState[flagpos.Item2, flagpos.Item1] == 'F' ? '?' : 'F';
                State = modifiedState;

                toRender = true;
            }

            if (TryParsePos(msg, out Tuple<int, int> pos)) {
                pos = ToRealPos(pos);
                if (!ProbeCell(pos.Item1, pos.Item2)) {
                    await message.Channel.SendMessageAsync($"That cell has already been probed!");
                    return;
                }

                toRender = true;
            }

            if (toRender) {
                if (board is not null) await board.DeleteAsync();
                string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
                string filepath = Path.Join(imageChacheDir, $"Minesweeper{game.Master}.png");
                System.Drawing.Image image = RenderMatrixImage(State);
                image.Save(filepath);
                IUserMessage newBoard = await message.Channel.SendFileAsync(filepath);
                BoardID = newBoard.Id;
                await message.DeleteAsync();
            }
        }
    }
}
