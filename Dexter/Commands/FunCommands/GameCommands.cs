using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Databases.Games;
using Discord;
using Discord.Commands;

namespace Dexter.Commands {
    partial class FunCommands {

        [Command("game")]
        
        //`CREATE` creates a new game session given a game type
        //`SET` sets variables within the game session (game master and staff only)
            //`Master` the game master
            //`Title` global game session title
            //`Description`
            //`Password`
            //game-specific values
        //`HELP|INFO` gives information about the currently selected game type
        //`JOIN` joins or requests joining a game session
        //`CLOSE` closes a game session (deletes all data)

        public async Task GameCommand(string Action, [Remainder] string Arguments = "") {

        }

        //`BAN` bans a user from a game session (game master and staff only)
        //`KICK` kicks a user from a game session
        //`SET`
            //`Score` sets a player's score
            //`Lives`

        [Command("player")]

        public async Task PlayerCommand(string Action, IGuildUser Player, [Remainder] string Arguments = "") {

        }

        public Player[] GetPlayersFromInstance(int InstanceID) {
            return GamesDB.Players.AsQueryable().Where(p => p.Playing == InstanceID).ToArray();
        }

        public void RemovePlayer(ulong PlayerID) {
            Player Player = GamesDB.Players.Find(PlayerID);
            
            if (Player is null) return;
            RemovePlayer(Player);
        }

        public void RemovePlayer(Player Player, bool SaveData = false) {
            Player.Score = 0;
            Player.Lives = 0;
            Player.Data = "";
            Player.Playing = -1;

            if (SaveData) GamesDB.SaveChanges();
        }

        public void BanPlayer(ulong PlayerID, int InstanceID) {

        }

        public bool Join(ulong PlayerID, int InstanceID, out string Error, string Password = "") {
            Error = "";
            
            GameInstance Instance = GamesDB.Games.Find(InstanceID);
            if (Instance is null) {
                Error = "Game Instance does not exist";
                return false;
            }

            string[] BannedIDs = Instance.Banned.Split(", ");
            foreach(string s in BannedIDs) {
                if (ulong.Parse(s) == PlayerID) {
                    Error = "Player is banned from this game";
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(Password) && Password != Instance.Password) {
                Error = "Password is incorrect";
                return false;
            }

            RemovePlayer(PlayerID);


        }

    }
}
