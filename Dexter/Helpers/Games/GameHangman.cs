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
    
    public class GameHangman : GameInstance, IGameTemplate {

        //Data structure: "term, guess, lives, maxlives, lettersmissed";
        const string EmptyData = "Default, _______, 5, 5,      ";

        public string Term { 
            get {
                return Data.Split(", ")[0].Replace(CommaRepresentation, ",");
            }
            set {
                string ProcessedValue = value.Replace(",", CommaRepresentation);
                string[] NewValue = Data.Split(", ");
                NewValue[0] = ProcessedValue;
                Data = string.Join(", ", NewValue);
            }
        }

        public string Guess {
            get {
                return Data.Split(", ")[1].Replace(CommaRepresentation, ",");
            }
            set {
                string ProcessedValue = value.Replace(",", CommaRepresentation);
                string[] NewValue = Data.Split(", ");
                NewValue[1] = ProcessedValue;
                Data = string.Join(", ", NewValue);
            }
        }

        public int Lives {
            get {
                return int.Parse(Data.Split(", ")[2]);
            }
            set {
                if (value < 0) return;
                string ProcessedValue = value.ToString();
                string[] NewValue = Data.Split(", ");
                NewValue[2] = ProcessedValue;
                Data = string.Join(", ", NewValue);
            }
        }

        public int MaxLives {
            get {
                return int.Parse(Data.Split(", ")[3]);
            }
            set {
                if (value < 1) return;
                string ProcessedValue = value.ToString();
                string[] NewValue = Data.Split(", ");
                NewValue[3] = ProcessedValue;
                Data = string.Join(", ", NewValue);
            }
        }

        public char[] LettersMissed {
            get {
                return Data.Split(", ")[4].ToCharArray();
            }
            set {
                string ProcessedValue = value.ToString();
                int ExpectedLength = MaxLives;
                if(ProcessedValue.Length < ExpectedLength) {
                    ProcessedValue = ProcessedValue.PadRight(ExpectedLength);
                } else if (ProcessedValue.Length > ExpectedLength) {
                    ProcessedValue = ProcessedValue[..ExpectedLength];
                }

                string[] NewValue = Data.Split(", ");
                NewValue[4] = ProcessedValue;
                Data = string.Join(", ", NewValue);
            }
        }

        public EmbedBuilder GetStatus(DiscordSocketClient Client) {
            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle($"{Title} (Game {GameID})")
                .WithDescription($"{Description}\n**Term**: {DiscordifyGuess()}")
                .AddField("Lives", LivesExpression(), true)
                .AddField("Wrong Guesses", string.Join(", ", LettersMissed), true)
                .AddField("Master", Client.GetUser(Master).GetUserInformation());
        }

        public void RegisterMistake(char c) {
            if (--Lives < 1) return;
            if (LettersMissed.Length < Lives) return;
            LettersMissed[^Lives] = c;
        }

        private string DiscordifyGuess() {
            return Guess.Replace("_", "\\_");
        }

        const char LifeFullChar = '♥';
        const char LifeEmptyChar = '☠';
        private string LivesExpression() {
            char[] Expression = new char[MaxLives];
            for(int i = 0; i < MaxLives; i++) {
                Expression[i] = i < Lives ? LifeFullChar : LifeEmptyChar;
            }
            return Expression.ToString();
        }

        public void Reset(FunConfiguration FunConfiguration) {
            Data = EmptyData;
            LastUserInteracted = Master;
            //Set lives and max lives to FunConfiguration value
        }

        public Task HandleMessage(SocketCommandContext Context, DiscordSocketClient Client, FunConfiguration FunConfiguration) {
            
            throw new NotImplementedException();
        }

        public bool Set(string Field, string Value, out string Error) {
            throw new NotImplementedException();
        }

        public EmbedBuilder Info(FunConfiguration FunConfiguration) {
            throw new NotImplementedException();
        }
    }
}
