using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;

namespace Dexter.Helpers.Games {

    /// <summary>
    /// Represents a game of Chess.
    /// </summary>

    public class GameChess : IGameTemplate {

        private const string EmptyData = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1, -, 0, 0, 0, 0, NN, standard";

        private string BoardRaw {
            get {
                return game.Data.Split(", ")[0];
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[0] = value;
                game.Data = string.Join(", ", newValue);
            }
        }

        private string LastMove {
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

        private ulong DumpID {
            get {
                return ulong.Parse(game.Data.Split(", ")[3]);
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[3] = value.ToString();
                game.Data = string.Join(", ", newValue);
            }
        }

        private ulong PlayerWhite {
            get {
                return ulong.Parse(game.Data.Split(", ")[4]);
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[4] = value.ToString();
                game.Data = string.Join(", ", newValue);
            }
        }

        private ulong PlayerBlack {
            get {
                return ulong.Parse(game.Data.Split(", ")[5]);
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[5] = value.ToString();
                game.Data = string.Join(", ", newValue);
            }
        }

        private string Agreements {
            get {
                return game.Data.Split(", ")[6];
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[6] = value;
                game.Data = string.Join(", ", newValue);
            }
        }

        private string Theme {
            get {
                return game.Data.Split(", ")[7];
            }
            set {
                string[] newValue = game.Data.Split(", ");
                newValue[7] = value;
                game.Data = string.Join(", ", newValue);
            }
        }

        private bool IsWhitesTurn {
            get {
                return BoardRaw.Split(" ")[1] == "w";
            }
        }

        private GameInstance game;

        /// <summary>
        /// Creates a new instance of a chess game given a generic GameInstance <paramref name="game"/>
        /// </summary>
        /// <param name="game">The generic GameInstance from which to generate the chess game.</param>

        public GameChess(GameInstance game) {
            this.game = game;
            if (string.IsNullOrEmpty(game.Data)) game.Data = EmptyData;
        }

        /// <summary>
        /// Represents the general status and data of a Chess Game.
        /// </summary>
        /// <param name="client">SocketClient used to parse UserIDs.</param>
        /// <returns>An Embed detailing the various aspects of the game in its current instance.</returns>

        public EmbedBuilder GetStatus(DiscordSocketClient client) {
            return new EmbedBuilder()
                .WithColor(Discord.Color.Blue)
                .WithTitle($"{game.Title} (Game {game.GameID})")
                .WithDescription($"{game.Description}")
                .AddField("White", $"<@{PlayerWhite}>", true)
                .AddField("Black", $"<@{PlayerBlack}>", true)
                .AddField("Turn", $"{(IsWhitesTurn ? "White" : "Black")}", true)
                .AddField("FEN Expression", BoardRaw)
                .AddField("Master", client.GetUser(game.Master).GetUserInformation())
                .AddField(game.Banned.Length > 0, "Banned Players", game.BannedMentions.TruncateTo(500));
        }

        /// <summary>
        /// Prints information about how to play Chess.
        /// </summary>
        /// <param name="funConfiguration">The configuration file holding relevant information for the game.</param>
        /// <returns>An <see cref="EmbedBuilder"/> object holding the stylized information.</returns>

        public EmbedBuilder Info(FunConfiguration funConfiguration) {
            return new EmbedBuilder()
                .WithColor(Discord.Color.Magenta)
                .WithTitle("How to Play: Chess")
                .WithDescription("owo");
        }

        /// <summary>
        /// Resets all data, except player color control, if given a <paramref name="gamesDB"/>, it will also reset their score and lives.
        /// </summary>
        /// <param name="funConfiguration">The configuration file holding all relevant parameters for Games.</param>
        /// <param name="gamesDB">The games database where relevant data concerning games and players is stored.</param>

        public void Reset(FunConfiguration funConfiguration, GamesDB gamesDB) {
            ulong white = PlayerWhite;
            ulong black = PlayerBlack;
            game.Data = EmptyData;
            PlayerWhite = white;
            PlayerBlack = black;
            if (gamesDB is not null) {
                foreach (Player p in gamesDB.GetPlayersFromInstance(game.GameID)) {
                    p.Score = 0;
                    p.Lives = 0;
                }
            }
        }

        /// <summary>
        /// Sets an internal game field <paramref name="field"/> to a given <paramref name="value"/>.
        /// </summary>
        /// <param name="field">The field to modify</param>
        /// <param name="value">The value to set <paramref name="field"/> to</param>
        /// <param name="funConfiguration">The configuration file holding relevant information about games settings.</param>
        /// <param name="feedback">The result of the operation, explained in a humanized way.</param>
        /// <returns><see langword="true"/> if the change was successful, otherwise <see langword="false"/>.</returns>

        public bool Set(string field, string value, FunConfiguration funConfiguration, out string feedback) {
            switch(field.ToLower()) {
                case "fen":
                case "pos":
                case "position":
                case "state":
                case "board":
                    if (!Board.TryParseBoard(value, out Board board, out feedback)) return false;
                    BoardRaw = board.ToString();
                    feedback = "Successfully set the value of board to the given value, type `board` to see the updated position.";
                    return true;
                case "theme":
                case "style":
                    if (!funConfiguration.ChessThemes.Contains(value)) {
                        feedback = $"Unable to find theme \"{value}\", valid themes are: {string.Join(", ", funConfiguration.ChessThemes)}.";
                        return false;
                    }
                    Theme = value.ToLower();
                    feedback = $"Successfully set theme to {value}";
                    return true;
            }

            feedback = $"Invalid field: \"{field}\" is not a default field nor \"board\" or \"theme\".";
            return false;
        }

        /// <summary>
        /// Handles a message sent in a games channel by a player currently playing in this game instance.
        /// </summary>
        /// <param name="message">The message sent by the player to be handled.</param>
        /// <param name="gamesDB">The database holding relevant information about games and players.</param>
        /// <param name="client">The Discord client used to send messages and parse users.</param>
        /// <param name="funConfiguration">The configuration settings attached to the Fun Commands module.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        public async Task HandleMessage(IMessage message, GamesDB gamesDB, DiscordSocketClient client, FunConfiguration funConfiguration) {
            if (message.Channel is IDMChannel) return;
            Player player = gamesDB.GetOrCreatePlayer(message.Author.Id);

            string msgRaw = message.Content.Replace("@", "@-");
            string msg = msgRaw.ToLower();
            IUserMessage boardMsg = null;
            if (BoardID != 0) boardMsg = await message.Channel.GetMessageAsync(BoardID) as IUserMessage;

            if (!Board.TryParseBoard(BoardRaw, out Board board, out string boardCorruptedError)) {
                await new EmbedBuilder()
                    .WithColor(Discord.Color.Red)
                    .WithTitle("Corrupted Board State")
                    .WithDescription($"Your current board state can't be parsed to a valid board, it has the following error:\n" +
                        $"{boardCorruptedError}\n" +
                        $"Feel free to reset the game using the `game reset` command or by setting a new board state in FEN notation with the `game set board [FEN]` command.")
                    .SendEmbed(message.Channel);
                return;
            }

            if (msg == "board") {
                bool lastMoveValid = Move.TryParseMock(LastMove, board, out Move lastMove, out string lastMoveError);
                if (!lastMoveValid) lastMove = null;

                if (boardMsg is not null) await boardMsg.DeleteAsync();
                IUserMessage newBoard = await message.Channel.SendMessageAsync(await CreateBoardDisplay(board, lastMove, client, funConfiguration));
                BoardID = newBoard.Id;
                return;
            }

            string[] args = msg.Split(" ");
            if (msg.StartsWith("claim")) {
                if (args.Length > 1) {
                    Player prevPlayer = null;
                    bool skip = false;
                    switch (args[1]) {
                        case "white":
                        case "w":
                            if (PlayerWhite == 0) skip = true;
                            else prevPlayer = gamesDB.Players.Find(PlayerWhite);
                            break;
                        case "black":
                        case "b":
                            if (PlayerBlack == 0) skip = true;
                            else prevPlayer = gamesDB.Players.Find(PlayerBlack);
                            break;
                        default:
                            await message.Channel.SendMessageAsync($"\"{args[1]}\" is not a valid color! Use 'B' or 'W'.");
                            return;
                    }

                    if (!skip && prevPlayer is not null && prevPlayer.Playing == game.GameID) {
                        await message.Channel.SendMessageAsync($"Can't claim color since player <@{prevPlayer.UserID}> is actively controlling it.");
                        return;
                    }

                    if (args[1].StartsWith("w")) PlayerWhite = message.Author.Id;
                    else PlayerBlack = message.Author.Id;

                    await message.Channel.SendMessageAsync($"<@{message.Author.Id}> will play with {(args[1].StartsWith("w") ? "white" : "black")}!");
                    return;
                }
                await message.Channel.SendMessageAsync("You need to specify what color you'd like to claim!");
                return;
            }

            if (Move.TryParse(msgRaw, board, out Move move, out string error)) {
                if (boardMsg is null) {
                    await message.Channel.SendMessageAsync($"You must create a board first! Type `board`.");
                    return;
                }

                if (!string.IsNullOrEmpty(error)) {
                    await message.Channel.SendMessageAsync(error);
                    return;
                }

                if ((board.isWhitesTurn && message.Author.Id != PlayerWhite) || (!board.isWhitesTurn && message.Author.Id != PlayerBlack)) {
                    await message.Channel.SendMessageAsync($"You don't control the {(board.isWhitesTurn ? "white" : "black")} pieces!");
                    return;
                }

                if (!move.IsLegal(board, out string legalerror)) {
                    await message.Channel.SendMessageAsync(legalerror);
                    return;
                }

                board.ExecuteMove(move);
                BoardRaw = board.ToString();
                LastMove = move.ToString();
                await message.DeleteAsync();

                string link = await CreateBoardDisplay(board, move, client, funConfiguration);
                await boardMsg.ModifyAsync(m => m.Content = link);

                Outcome outcome = board.GetOutcome();
                if (outcome == Outcome.Checkmate) {
                    BoardID = 0;
                    player.Score += 1;
                    await new EmbedBuilder()
                        .WithColor(Discord.Color.Green)
                        .WithTitle($"{(board.isWhitesTurn ? "White" : "Black")} wins!")
                        .WithDescription("Create a new board if you wish to play again, or pass your color control to a different player.")
                        .SendEmbed(message.Channel);
                    Reset(funConfiguration, null);
                    return;
                }

                if (outcome == Outcome.Draw) {
                    await new EmbedBuilder()
                        .WithColor(Discord.Color.LightOrange)
                        .WithTitle("Draw!")
                        .WithDescription("Stalemate reached! Create a new board if you wish to play again, or pass your color control to a different player.")
                        .SendEmbed(message.Channel);
                    Reset(funConfiguration, null);
                    return;
                }

                return;
            }

            if (args.Length > 2 && args[0] == "pass") {
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

                switch (args[1]) {
                    case "w":
                    case "white":
                        if (message.Author.Id != game.Master && message.Author.Id != PlayerWhite) {
                            await message.Channel.SendMessageAsync($"You aren't the master nor controlling the white pieces!");
                            return;
                        }
                        PlayerWhite = otherPlayer.UserID;
                        break;
                    case "b":
                    case "black":
                        if (message.Author.Id != game.Master && message.Author.Id != PlayerBlack) {
                            await message.Channel.SendMessageAsync($"You aren't the master nor controlling the black pieces!");
                            return;
                        }
                        PlayerBlack = otherPlayer.UserID;
                        break;
                    default:
                        await message.Channel.SendMessageAsync($"Unable to parse \"{args[1]}\" into a valid color!");
                        return;
                }

                await message.Channel.SendMessageAsync($"{otherUser.Mention} now controls the {(args[1][0] == 'w' ? "white" : "black")} pieces!");
                return;
            }

            if (args[0] == "swap") {
                if (message.Author.Id != game.Master) {
                    await message.Channel.SendMessageAsync("Only the game master can swap the colors!");
                    return;
                }

                ulong temp = PlayerWhite;
                PlayerWhite = PlayerBlack;
                PlayerBlack = temp;
                await message.Channel.SendMessageAsync($"Colors have been swapped!\n" +
                    $"WHITE: <@{PlayerWhite}>\n" +
                    $"BLACK: <@{PlayerBlack}>");
                return;
            }
        }

        private async Task<string> CreateBoardDisplay(Board board, Move lastMove, DiscordSocketClient client, FunConfiguration funConfiguration) {
            string imageChacheDir = Path.Combine(Directory.GetCurrentDirectory(), "ImageCache");
            string filepath = Path.Join(imageChacheDir, $"Chess{game.Master}.png");
            System.Drawing.Image image = RenderBoard(board, lastMove);
            image.Save(filepath);
            if (DumpID != 0) { 
                IMessage prevDump = await (client.GetChannel(funConfiguration.GamesImageDumpsChannel) as ITextChannel).GetMessageAsync(DumpID);
                if (prevDump is not null) await prevDump.DeleteAsync(); 
            }
            IUserMessage cacheMessage = await (client.GetChannel(funConfiguration.GamesImageDumpsChannel) as ITextChannel).SendFileAsync(filepath);
            DumpID = cacheMessage.Id;

            return cacheMessage.Attachments.First().ProxyUrl;
        }

        private System.Drawing.Image RenderBoard(Board board, Move lastMove) {
            Bitmap img = new Bitmap(2 * Offset + 8 * CellSize, 2 * Offset + 8 * CellSize);

            Dictionary<char, System.Drawing.Image> pieceImages = new();
            foreach (Piece p in Piece.pieces) {
                for (int c = 0; c < 2; c++) {
                    pieceImages.Add(c == 0 ? p.representation : char.ToLower(p.representation), 
                        System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{PiecePrefixes[c]}{p.representation}.png")));
                }
            }

            using (Graphics g = Graphics.FromImage(img)) {
                using (System.Drawing.Image boardImg = System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{BoardImgName}.png"))) {
                    g.DrawImage(boardImg, 0, 0, 2 * Offset + 8 * CellSize, 2 * Offset + 8 * CellSize);
                }

                if (lastMove != null) {
                    using (System.Drawing.Image highlight = System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{HighlightImage}.png"))) {
                        foreach(int n in lastMove.ToHighlight()) {
                            g.DrawImage(highlight, Offset + (n % 8) * CellSize, Offset + (n / 8) * CellSize, CellSize, CellSize);
                        }
                    }

                    using (System.Drawing.Image danger = System.Drawing.Image.FromFile(Path.Join(ChessPath, Theme, $"{DangerImage}.png"))) {
                        foreach (int n in lastMove.ToDanger(board)) {
                            g.DrawImage(danger, Offset + (n % 8) * CellSize, Offset + (n / 8) * CellSize, CellSize, CellSize);
                        }
                    }
                }

                for (int x = 0; x < 8; x++) {
                    for (int y = 0; y < 8; y++) {
                        if (board.squares[x, y] != '-') {
                            g.DrawImage(pieceImages[board.squares[x, y]], Offset + CellSize * x, Offset + CellSize * y, CellSize, CellSize);
                        }
                    }
                }
            }

            return img;
        }

        const int CellSize = 64;
        const int Offset = 32;
        private const string ChessPath = "Images/Games/Chess";
        private const string BoardImgName = "Board";
        private const string HighlightImage = "SquareHighlight";
        private const string DangerImage = "SquareDanger";
        private readonly string[] PiecePrefixes = new string[] {"W", "B"};

        private static Tuple<int, int> ToMatrixCoords(int pos) {
            return new Tuple<int, int>(pos % 8, pos / 8);
        }

        private static string ToSquareName(Tuple<int, int> coord) {
            return $"{(char)('a' + coord.Item1)}{(char)('8' - coord.Item2)}";
        }

        private static bool TryParseSquare(string input, out int pos) {
            pos = -1;
            if (input.Length < 2) return false;

            if (input[0] < 'a' || input[0] > 'h') return false;
            pos = input[0] - 'a';

            if (input[1] < '1' || input[1] > '8') return false;
            pos += ('8' - input[1]) * 8;

            return true;
        }

        [Flags]
        private enum MoveType {
            None = 0,
            Orthogonal = 1,
            Diagonal = 2,
            Direct = 4,
            Pawn = 8
        }

        private class Piece {
            public char representation;
            public string name;
            public Func<int, int, Board, bool> isValid;

            public static readonly Piece Rook = new Piece() {
                representation = 'R',
                name = "Rook",
                isValid = (origin, target, board) => {
                    if (!BasicValidate(Rook, origin, target, board)) return false;
                    return OrthogonalValidate(origin, target, board);
                }
            };

            public static readonly Piece Knight = new Piece() {
                representation = 'N',
                name = "Knight",
                isValid = (origin, target, board) => {
                    if (!BasicValidate(Knight, origin, target, board)) return false;
                    int xdiff = Math.Abs(target % 8 - origin % 8);
                    int ydiff = Math.Abs(target / 8 - origin / 8);
                    if (xdiff >= 3 || ydiff >= 3) return false;
                    return xdiff + ydiff == 3;
                }
            };

            public static readonly Piece Bishop = new Piece() {
                representation = 'B',
                name = "Bishop",
                isValid = (origin, target, board) => {
                    if (!BasicValidate(Bishop, origin, target, board)) return false;
                    return DiagonalValidate(origin, target, board);
                }
            };

            public static readonly Piece King = new Piece() {
                representation = 'K',
                name = "King",
                isValid = (origin, target, board) => {
                    if (!BasicValidate(King, origin, target, board)) return false;
                    int xdiff = Math.Abs(target % 8 - origin % 8);
                    int ydiff = Math.Abs(target / 8 - origin / 8);
                    return xdiff <= 1 && ydiff <= 1;
                }
            };

            public static readonly Piece Queen = new Piece() {
                representation = 'Q',
                name = "Queen",
                isValid = (origin, target, board) => {
                    if (!BasicValidate(Queen, origin, target, board)) return false;
                    return OrthogonalValidate(origin, target, board) || DiagonalValidate(origin, target, board);
                }
            };

            public static readonly Piece Pawn = new Piece() {
                representation = 'P',
                name = "Pawn",
                isValid = (origin, target, board) => {
                    if (!BasicValidate(Pawn, origin, target, board)) return false;
                    int yAdv = board.isWhitesTurn ? -1 : 1;
                    int x0 = origin % 8;
                    int y0 = origin / 8;
                    int xf = target % 8;
                    int yf = target / 8;
                    if (board.GetSquare(target) != '-' || (target == board.enPassant && x0 != xf)) {
                        return Math.Abs(x0 - xf) == 1 && yf == y0 + yAdv;
                    } else {
                        if (xf != x0) return false;
                        if (yf == y0 + yAdv) return true;
                        int initialRank = board.isWhitesTurn ? 6 : 1;
                        return y0 == initialRank && yf == y0 + 2 * yAdv && board.squares[x0, y0 + yAdv] == '-';
                    }
                }
            };

            private static bool BasicValidate(Piece p, int origin, int target, Board board) {
                if (origin == target) return false;
                if (char.ToUpper(board.GetSquare(origin)) != p.representation) return false;
                char piecef = board.GetSquare(target);
                if (piecef != '-' && (char.IsLower(piecef) ^ board.isWhitesTurn)) return false;
                return true;
            }

            private static bool OrthogonalValidate(int origin, int target, Board board) {
                int x0 = origin % 8;
                int y0 = origin / 8;
                int xf = target % 8;
                int yf = target / 8;
                if (y0 == yf) {
                    int direction = target - origin > 0 ? 1 : -1;
                    int x = x0 + direction;
                    while (x != xf) {
                        if (board.squares[x, y0] != '-') return false;
                        x += direction;
                    }
                    return true;
                }
                else if (x0 == xf) {
                    int direction = target - origin > 0 ? 1 : -1;
                    int y = y0 + direction;
                    while (y != yf) {
                        if (board.squares[x0, y] != '-') return false;
                        y += direction;
                    }
                    return true;
                }

                return false;
            }

            private static bool DiagonalValidate(int origin, int target, Board board) {
                int x0 = origin % 8;
                int y0 = origin / 8;
                int xf = target % 8;
                int yf = target / 8;
                int ydir = target - origin > 0 ? 1 : -1;
                int xdir;
                if (x0 + y0 == xf + yf) {
                    xdir = -ydir;
                }
                else if (x0 - y0 == xf - yf) {
                    xdir = ydir;
                }
                else return false;

                int x = x0 + xdir;
                int y = y0 + ydir;
                while (x != xf) {
                    if (board.squares[x, y] != '-') return false;
                    x += xdir;
                    y += ydir;
                }

                return true;
            }

            public static readonly Piece[] pieces = new Piece[] { Rook, Knight, Bishop, King, Queen, Pawn };
            public static char[] PieceCharacters {
                get {
                    char[] result = new char[pieces.Length];
                    for (int i = 0; i < result.Length; i++) {
                        result[i] = pieces[i].representation;
                    }
                    return result;
                }
            }

            public static Piece FromRepresentation(char c) {
                foreach (Piece p in pieces) {
                    if (p.representation == char.ToUpper(c)) return p;
                }
                return null;
            }
        }

        /// <summary>
        /// Represents a move in chess
        /// </summary>

        private class Move {
            public int origin;
            public int target;
            public bool isCastle;
            public bool isCapture;
            public bool isEnPassant;
            public bool isCheck;
            public bool isCheckMate;
            public char promote;

            public bool IsLegal(Board board, out string error) {
                error = "";
                return true;
            }

            public static bool TryParseMock(string input, Board board, out Move move, out string error) {
                move = new Move(-1, -1);
                error = "";
                if (Regex.IsMatch(input.ToUpper(), @"^[O0]\-[O0]([+#!?.\s]|$)")) {
                    move.origin = !board.isWhitesTurn ? 60 : 4;
                    move.target = !board.isWhitesTurn ? 62 : 6;
                    move.isCastle = true;
                    return true;
                }
                else if (Regex.IsMatch(input.ToUpper(), @"^[O0]\-[O0]\-[O0]([+#!?.\s]|$)")) {
                    move.origin = !board.isWhitesTurn ? 60 : 4;
                    move.target = !board.isWhitesTurn ? 58 : 2;
                    move.isCastle = true;
                    return true;
                }
                else if (input.Length != 4) { error = "wrong format"; return false; }

                if (!TryParseSquare(input[..2], out move.origin)) {
                    error = "wrong format on origin"; return false;
                }
                if (!TryParseSquare(input[^2..], out move.target)) {
                    error = "wrong format on target"; return false;
                }

                return true;
            }

            public static bool TryParse(string input, Board board, out Move move, out string error) {
                move = new(-1, -1);
                error = "";

                Match promotionSegment = Regex.Match(input, @"=[A-Z]([+#!?.]|$)");
                if (promotionSegment.Success) {
                    move.promote = promotionSegment.Value[1];
                    if (!Piece.PieceCharacters.Contains(move.promote)) {
                        error = $"\"{move.promote}\" cannot be parsed to a valid piece!";
                        return true;
                    }
                    if (move.promote == Piece.King.representation || move.promote == Piece.Pawn.representation) {
                        error = $"You can't promote a piece to a King or a Pawn.";
                        return true;
                    }
                }

                if (Regex.IsMatch(input.ToUpper(), @"^[O0]\-[O0]([+#!?.\s]|$)")) {
                    if (!board.castling[board.isWhitesTurn ? 0 : 2]) {
                        error = "Short castling is currently unavailable!";
                        return true;
                    }
                    move.origin = board.isWhitesTurn ? 60 : 4;
                    move.target = board.isWhitesTurn ? 62 : 6;
                    move.isCastle = true;
                    int dir = move.target - move.origin > 0 ? 1 : -1;
                    int rook = board.isWhitesTurn ? 63 : 7;
                    for (int pos = move.origin + dir; pos != rook; pos += dir) {
                        if (board.GetSquare(pos) != '-') {
                            error = $"Can't castle king-side! A piece is obstructing the castle in {ToSquareName(ToMatrixCoords(pos))}.";
                            return true;
                        }
                    }
                    return true;
                }
                else if (Regex.IsMatch(input.ToUpper(), @"^[O0]\-[O0]\-[O0]([+#!?.\s]|$)")) {
                    if (!board.castling[board.isWhitesTurn ? 1 : 3]) {
                        error = "Long castling is currently unavailable!";
                        return true;
                    }
                    move.origin = board.isWhitesTurn ? 60 : 4;
                    move.target = board.isWhitesTurn ? 58 : 2;
                    move.isCastle = true;
                    int dir = move.target - move.origin > 0 ? 1 : -1;
                    int rook = board.isWhitesTurn ? 56 : 0;
                    for (int pos = move.origin + dir; pos != rook; pos += dir) {
                        if (board.GetSquare(pos) != '-') {
                            error = $"Can't castle queen-side! A piece is obstructing the castle in {ToSquareName(ToMatrixCoords(pos))}.";
                            return true;
                        }
                    }
                    return true;
                }

                List<int> potentialOrigins = new();
                int rankFilter = -1;
                int fileFilter = -1;
                Piece toMove;
                Match explicitFormMatch = Regex.Match(input, @"^[a-h][1-8][x\s]*[a-hA-H][1-8]");

                if (explicitFormMatch.Success) {
                    if (!TryParseSquare(explicitFormMatch.Value[..2], out move.origin)) {
                        error = $"The specified origin square ({explicitFormMatch.Value[..2]}) is invalid!";
                        return true;
                    }
                    potentialOrigins.Add(move.origin);
                    if (!Piece.PieceCharacters.Contains(char.ToUpper(board.GetSquare(move.origin)))) {
                        error = $"The specified origin square ({explicitFormMatch.Value[..2]}) doesn't contain a valid piece";
                        return true;
                    }
                    toMove = Piece.FromRepresentation(board.GetSquare(move.origin));

                    if (!TryParseSquare(explicitFormMatch.Value[^2..].ToLower(), out move.target)) {
                        error = $"The specified target square ({explicitFormMatch.Value[^2..]}) is invalid!";
                        return true;
                    }
                    if (!toMove.isValid(move.origin, move.target, board)) {
                        error = "The targeted piece cannot move to the desired square!";
                        return true;
                    }
                } else {
                    Match basicFormMatch = Regex.Match(input, @"^[A-Z]?[a-h1-8]?x?[a-hA-H][1-8]");

                    if (basicFormMatch.Success) {
                        string basicForm = basicFormMatch.Value;

                        if (!TryParseSquare(basicForm[^2..].ToLower(), out move.target)) {
                            error = $"The specified target square ({basicForm[^2..]}) is invalid!";
                            return true;
                        }

                        if (char.IsLower(basicForm[0])) toMove = Piece.Pawn;
                        else if (!Piece.PieceCharacters.Contains(input[0])) {
                            error = $"\"{input[0]}\" cannot be parsed to a valid piece!";
                            return true;
                        }
                        else toMove = Piece.FromRepresentation(input[0]);

                        if (Regex.IsMatch(basicForm, @"^[A-Z]?[a-h]x?[a-hA-H][1-8]")) {
                            fileFilter = (char.IsLower(basicForm[0]) ? basicForm[0] : basicForm[1]) - 'a';
                        }
                        else if (Regex.IsMatch(basicForm, @"^[A-Z][1-8]x?[a-hA-H][1-8]")) {
                            rankFilter = '8' - basicForm[1];
                        }

                        char targetPiece = board.isWhitesTurn ? toMove.representation : char.ToLower(toMove.representation);

                        for (int x = 0; x < 8; x++) {
                            if (fileFilter != -1 && fileFilter != x) continue;
                            for (int y = 0; y < 8; y++) {
                                if (rankFilter != -1 && rankFilter != y) continue;
                                if (board.squares[x, y] == targetPiece) potentialOrigins.Add(x + y * 8);
                            }
                        }

                        List<int> validOrigins = new();
                        foreach (int origin in potentialOrigins) {
                            if (toMove.isValid(origin, move.target, board)) {
                                validOrigins.Add(origin);
                            }
                        }

                        if (validOrigins.Count == 0) {
                            error = $"Invalid move! Unable to find any {toMove.name}" +
                                $"{(fileFilter == -1 ? "" : $" in file {(char)('a' + fileFilter)}")}" +
                                $"{(rankFilter == -1 ? "" : $" in rank {(char)('8' - rankFilter)}")}" +
                                $" which can move to {ToSquareName(ToMatrixCoords(move.target))}";
                            return true;
                        }
                        else if (validOrigins.Count > 1) {
                            string[] ambiguousSquares = new string[validOrigins.Count];
                            for (int i = 0; i < ambiguousSquares.Length; i++) {
                                ambiguousSquares[i] = ToSquareName(ToMatrixCoords(validOrigins[i]));
                            }
                            error = $"Ambiguous move! A {toMove.name} can move to that position from: {string.Join(", ", ambiguousSquares)}";
                            return true;
                        }

                        move.origin = validOrigins[0];
                    }
                    else return false;
                }

                if (move.origin >= 0 && move.target >= 0) {
                    if (((move.target / 8 == 0 && board.isWhitesTurn) || (move.target / 8 == 7 && !board.isWhitesTurn))
                        && (toMove == Piece.Pawn)) {
                        if (move.promote == default) move.promote = Piece.Queen.representation;
                    }
                    else
                        move.promote = ' ';
                }

                if (move.target == board.enPassant && toMove == Piece.Pawn && board.GetSquare(move.target) == '-' && (move.target - move.origin) % 8 != 0) { move.isEnPassant = true; move.isCapture = true; }
                if (board.GetSquare(move.target) != '-') move.isCapture = true;

                return true;
            }

            public Move(int origin, int target, bool isCastle = false, bool isCapture = false, bool isEnPassant = false, bool isCheck = false, bool isCheckMate = false, char promote = ' ') {
                this.origin = origin;
                this.target = target;
                this.isCastle = isCastle;
                this.isCapture = isCapture;
                this.isEnPassant = isEnPassant;
                this.isCheck = isCheck;
                this.isCheckMate = isCheckMate;
                this.promote = promote;
            }

            public List<int> ToHighlight() {
                List<int> result = new();

                result.Add(origin);
                result.Add(target);

                if (isCastle) {
                    if (target % 8 < 4) {
                        result.Add(target + 1);
                        result.Add(target - 2);
                    }
                    else {
                        result.Add(target - 1);
                        result.Add(target + 1);
                    }
                }
                return result;
            }

            public List<int> ToDanger(Board board) {
                List<int> result = new();
                if (isCheck || isCheckMate) {
                    result.Add(board.isWhitesTurn ? board.whiteKing : board.blackKing);
                }
                return result;
            }

            /// <summary>
            /// Stringifies the move.
            /// </summary>
            /// <returns>A string representing the origin and endpoint of the move.</returns>

            public override string ToString() {
                if (isCastle) {
                    return target > origin ? "O-O" : "O-O-O";
                }
                Tuple<int, int> originpos = ToMatrixCoords(origin);
                Tuple<int, int> finalpos = ToMatrixCoords(target);
                return $"{ToSquareName(originpos)}{ToSquareName(finalpos)}";
            }
        }

        private enum Outcome {
            Playing,
            Draw,
            Checkmate
        }

        private class Board {
            public char[,] squares;
            public bool isWhitesTurn;
            public bool[] castling;
            public int enPassant;
            public int halfmoves;
            public int fullmoves;

            public int whiteKing = -1;
            public int blackKing = -1;

            public static bool TryParseBoard(string fen, out Board board, out string error) {
                error = "";
                board = new Board();
                string[] components = fen.Split(" ");

                if (components.Length < 6) {
                    error = "Missing components! The syntax must be `positions turn castling enpassant halfmoves fullmoves`";
                    return false;
                }

                string[] ranks = components[0].Split('/');

                if (ranks.Length != 8) {
                    error = "Positions expression doesn't have the correct number of ranks separated by '/'";
                    return false;
                }

                char[] pieceChars = Piece.PieceCharacters;

                board.squares = new char[8, 8];
                for(int i = 0; i < 8; i++) {
                    int counter = 0;
                    foreach (char c in ranks[i]) {
                        if (counter >= 8) {
                            error = $"Rank {8 - i} contains more than 8 positions.";
                            return false;
                        }
                        if (!int.TryParse(c.ToString(), out int n)) {
                            if (!pieceChars.Contains(char.ToUpper(c))) {
                                error = $"Rank {8 - i} contains an invalid piece: \"{c}\"";
                                return false;
                            }
                            if (c == 'K') {
                                if (board.whiteKing >= 0) {
                                    error = $"This board contains more than one white king!";
                                    return false;
                                }
                                board.whiteKing = i * 8 + counter;
                            }
                            else if (c == 'k') {
                                if (board.blackKing >= 0) {
                                    error = $"This board contains more than one black king!";
                                    return false;
                                }
                                board.blackKing = i * 8 + counter;
                            }
                            board.squares[counter++, i] = c;
                            continue;
                        }
                        if (counter + n > 8) {
                            error = $"Rank {8 - i} contains more than 8 positions.";
                            return false;
                        }
                        for (int j = 0; j < n; j++) {
                            board.squares[counter++, i] = '-';
                        }
                    }
                }

                if (board.whiteKing < 0 || board.blackKing < 0) {
                    error = $"Both colors must at least have a king!";
                    return false;
                }

                board.isWhitesTurn = components[1] == "w";

                board.castling = new bool[] { components[2].Contains('K'),
                    components[2].Contains('Q'),
                    components[2].Contains('k'),
                    components[2].Contains('q')
                };

                if (board.squares[0, 0] != 'r') board.castling[3] = false;
                if (board.squares[4, 0] != 'k') { board.castling[2] = false; board.castling[3] = false; }
                if (board.squares[7, 0] != 'r') board.castling[2] = false;
                if (board.squares[0, 7] != 'R') board.castling[1] = false;
                if (board.squares[4, 7] != 'K') { board.castling[0] = false; board.castling[1] = false; }
                if (board.squares[7, 7] != 'R') board.castling[0] = false;

                if (components[3] == "-") board.enPassant = -1;
                else if (!TryParseSquare(components[3], out board.enPassant)) {
                    error = "Unable to parse en-passant square into a valid square (a1-h8)";
                    return false;
                }

                if (!int.TryParse(components[4], out board.halfmoves)) {
                    error = "Unable to parse halfmoves into an integer.";
                    return false;
                }

                if (!int.TryParse(components[5], out board.fullmoves)) {
                    error = "Unable to parse fullmoves into an integer.";
                    return false;
                }

                return true;
            }

            public void ExecuteMove(Move move) {
                int x0 = move.origin % 8;
                int y0 = move.origin / 8;
                int xf = move.target % 8;
                int yf = move.target / 8;
                char representation = squares[x0, y0];
                squares[x0, y0] = '-';
                squares[xf, yf] = representation;

                if (move.isEnPassant) squares[xf, y0] = '-';
                if (move.isCastle) {
                    squares[xf < 4 ? 0 : 7, yf] = '-';
                    squares[(x0 + xf) / 2, yf] = 'r'.MatchCase(representation);
                }
                bool isPawn = char.ToUpper(representation) == Piece.Pawn.representation;
                if (isPawn && Math.Abs(yf - y0) == 2)
                    enPassant = (move.target + move.origin) / 2;
                else
                    enPassant = -1;

                isWhitesTurn = !isWhitesTurn;
                if (isWhitesTurn) fullmoves++;
                if (isPawn || move.isCapture) halfmoves = 0;
                else halfmoves++;
            }

            public Outcome GetOutcome() {
                // if halfmoves >= 100 => draw
                // if insufficient material => draw
                // if checkmate => Checkmate
                return Outcome.Playing;
                throw new NotImplementedException();
            }

            public char GetSquare(int value) {
                return squares[value % 8, value / 8];
            }

            public override string ToString() {
                StringBuilder builder = new StringBuilder(80);

                for (int y = 0; y < 8; y++) {
                    if (y != 0) builder.Append('/');
                    int spaces = 0;
                    for (int x = 0; x < 8; x++) {
                        if (squares[x, y] == '-') { spaces++; continue; }
                        if (spaces > 0) { builder.Append(spaces); spaces = 0; }
                        builder.Append(squares[x, y]);
                    }
                    if (spaces > 0) builder.Append(spaces);
                }

                builder.Append($" {(isWhitesTurn ? 'w' : 'b')} ");

                if (!castling[0] && !castling[1] && !castling[2] && !castling[3]) builder.Append('-');
                else {
                    if (castling[0]) builder.Append('K');
                    if (castling[1]) builder.Append('Q');
                    if (castling[2]) builder.Append('k');
                    if (castling[3]) builder.Append('q');
                }
                builder.Append(' ');

                string enPassantExpression = "-";
                if (enPassant >= 0) {
                    enPassantExpression = ToSquareName(ToMatrixCoords(enPassant));
                }

                builder.Append($"{enPassantExpression} {halfmoves} {fullmoves}");

                return builder.ToString();
            }

        }
    }
}
