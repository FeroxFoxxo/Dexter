using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Databases.UserProfiles;
using Discord;
using Discord.WebSocket;

namespace Dexter.Services {

    /// <summary>
    /// Stores, manages, and records modifications to users' nicknames among other data.
    /// </summary>

    public class UserRecordsService : Service {

        /// <summary>
        /// Holds all information used in the module for management.
        /// </summary>

        public ProfilesDB ProfilesDB { get; set; }

        /// <summary>
        /// This method is called after all dependencies are initialized and serves to hook the appropriate events to this service's methods.
        /// </summary>

        public override void Initialize() {
            DiscordSocketClient.GuildMemberUpdated += RecordNameChange;
            DiscordSocketClient.UserJoined += RecordUserJoin;
        }

        /// <summary>
        /// Adds a new nickname to the User's profile nicknames tracker if <paramref name="Before"/> and <paramref name="After"/> have different nicknames and the nickname isn't already recorded.
        /// </summary>
        /// <param name="Before">The information of the user before the modification.</param>
        /// <param name="After">The information of the user after the modification.</param>
        /// <returns>A completed <c>Task</c> object.</returns>

        public async Task RecordNameChange(SocketGuildUser Before, SocketGuildUser After) {

            if (Before.Username != After.Username && !string.IsNullOrEmpty(After.Username)) {
                NameRecord Record = ProfilesDB.Names.AsQueryable().Where(n => n.Name == After.Username && n.UserID == After.Id && n.Type == NameType.Username).FirstOrDefault();

                if (Record != null)
                    Record.SetTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                else {
                    ProfilesDB.Names.Add(new() {
                        UserID = After.Id,
                        Name = After.Username,
                        SetTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                        Type = NameType.Username
                    });
                }
                await ProfilesDB.SaveChangesAsync();
            }

            if (Before.Nickname != After.Nickname && !string.IsNullOrEmpty(After.Nickname)) {
                NameRecord Record = ProfilesDB.Names.AsQueryable().Where(n => n.Name == After.Username && n.UserID == After.Id && n.Type == NameType.Nickname).FirstOrDefault();

                if (Record != null)
                    Record.SetTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                else {
                    ProfilesDB.Names.Add(new() {
                        UserID = After.Id,
                        Name = After.Nickname,
                        SetTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                        Type = NameType.Nickname
                    });
                }
                await ProfilesDB.SaveChangesAsync();
            }

            return;
        }

        /// <summary>
        /// Removes all names on <paramref name="User"/> following a given <paramref name="Pattern"/> from their records.
        /// </summary>
        /// <param name="User">The target user.</param>
        /// <param name="NameType">Whether to target NICKNAMEs or USERNAMEs.</param>
        /// <param name="Pattern">The pattern to follow deletion, a regular expression if <paramref name="IsRegex"/>, otherwise the verbatim nickname.</param>
        /// <param name="IsRegex">Whether <paramref name="Pattern"/> should be interpreted as a regular expression.</param>
        /// <returns>A List of strings containing all nicknames that fit <paramref name="Pattern"/>.</returns>

        public List<NameRecord> RemoveNames(IUser User, NameType NameType, string Pattern, bool IsRegex = false) {
            List<NameRecord> Result = new();
            List<NameRecord> Records = ProfilesDB.Names.Where(n => n.Type == NameType && n.UserID == User.Id).ToList();
            Pattern = Pattern.Trim();

            if (IsRegex) {
                foreach(NameRecord Record in Records) {
                    if (Regex.Match(Record.Name, Pattern).Success) {
                        ProfilesDB.Names.Remove(Record);
                        Result.Add(Record.Clone());
                    }
                }
            } else {
                foreach(NameRecord Record in Records) {
                    if (Record.Name == Pattern) {
                        ProfilesDB.Names.Remove(Record);
                        Result = new List<NameRecord>() { Record.Clone() };
                        break;
                    }
                }
            }

            ProfilesDB.SaveChanges();
            return Result;
        }

        /// <summary>
        /// Gets all historically recorded <paramref name="NameType"/>s for a given <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The target user to fetch for.</param>
        /// <param name="NameType">Whether to query for Nicknames or Usernames.</param>
        /// <returns>An array of strings containing all <paramref name="NameType"/>s <paramref name="User"/> has used.</returns>

        public string[] GetNames(IUser User, NameType NameType) {
            NameRecord[] NameRecords = GetNameRecords(User, NameType);

            string[] Names = new string[NameRecords.Length];

            for(int i = 0; i < Names.Length; i++) {
                Names[i] = NameRecords[i].Name;
            }

            return Names;
        }

        /// <summary>
        /// Gets all historically recorded <paramref name="NameType"/> Records for a given <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The target user to fetch for.</param>
        /// <param name="NameType">Whether to query for Nicknames or Usernames.</param>
        /// <returns>An array of NameRecords containing all <paramref name="NameType"/>s <paramref name="User"/> has used.</returns>

        public NameRecord[] GetNameRecords(IUser User, NameType NameType) {
            return ProfilesDB.Names.AsQueryable().Where(n => n.Type == NameType && n.UserID == User.Id).ToArray();
        }

        /// <summary>
        /// Saves the first time a user joined the guild, and does nothing on subsequent events.
        /// </summary>
        /// <param name="User">The user whose join event is being recorded.</param>
        /// <returns>A completed <c>Task</c> object.</returns>

        public Task RecordUserJoin(SocketGuildUser User) {
            UserProfile Profile = ProfilesDB.GetOrCreateProfile(User.Id);

            if (Profile.DateJoined != default) return Task.CompletedTask;

            Profile.DateJoined = DateTimeOffset.Now.ToUnixTimeSeconds();
            ProfilesDB.SaveChanges();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Obtains the time the user joined the server for the first time, if no information exists it is created.
        /// </summary>
        /// <remarks>Using this command on a user whose record doesn't exist and whose join date can't be obtained will return <see langword="default"/>.</remarks>
        /// <param name="User">The user whose join date is being queried.</param>
        /// <returns>A <c>DateTimeOffset</c> object detailing the join date of the user, or <see langword="default"/> if no data can be obtained.</returns>

        public DateTimeOffset GetUserJoin(IGuildUser User) {
            UserProfile Profile = ProfilesDB.GetOrCreateProfile(User.Id);

            if (Profile.DateJoined != default) return DateTimeOffset.FromUnixTimeSeconds(Profile.DateJoined);

            Profile.DateJoined = User.JoinedAt?.ToUnixTimeSeconds() ?? default;
            ProfilesDB.SaveChanges();

            return Profile.DateJoined == default ? default : DateTimeOffset.FromUnixTimeSeconds(Profile.DateJoined);
        }
    }
}
