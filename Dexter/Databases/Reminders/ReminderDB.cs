using System;
using System.Linq;
using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Reminders {

    /// <summary>
    /// Contains all reminders and relevant management methods for the Reminder System.
    /// </summary>

    public class ReminderDB : Database {

        /// <summary>
        /// Holds all individual reminders and their related information for processing.
        /// </summary>

        public DbSet<Reminder> Reminders { get; set; }

        /// <summary>
        /// Gets a reminder from the Database by its <paramref name="ReminderID"/>.
        /// </summary>
        /// <param name="ReminderID">The ID of the reminder to fetch.</param>
        /// <returns>A <c>Reminder</c> object if one with the given ID exists, <see langword="null"/> otherwise.</returns>

        public Reminder GetReminder(int ReminderID) {
            return Reminders.Find(ReminderID);
        }

        /// <summary>
        /// Gets a list of all reminders filtered by IssuerID, where the Issuer is <paramref name="User"/>.
        /// </summary>
        /// <param name="User">The Issuer of reminders to filter for.</param>
        /// <returns>A <c>Reminder[]</c> array, where all Issuers are <paramref name="User"/>.</returns>

        public Reminder[] GetRemindersByUser(Discord.IUser User) {
            return GetRemindersByUser(User.Id);
        }

        /// <summary>
        /// Gets a list of all reminders filtered by IssuerID, where the IssuerID is <paramref name="UserID"/>.
        /// </summary>
        /// <param name="UserID">The ID of the Issuer of reminders to filter for.</param>
        /// <returns>A <c>Reminder[]</c> array, where all IssuerIDs are <paramref name="UserID"/>.</returns>

        public Reminder[] GetRemindersByUser(ulong UserID) {
            return Reminders.AsQueryable().Where(r => r.IssuerID == UserID).ToArray();
        }

        /// <summary>
        /// Creates a new reminder from given parameters, adds it to the database, and returns it.
        /// </summary>
        /// <param name="User">The Issuer of the reminder.</param>
        /// <param name="Time">The time the reminder should release at.</param>
        /// <param name="Message">The content of the reminder.</param>
        /// <returns>A <c>Reminder</c> object which includes the assigned ID.</returns>

        public Reminder AddReminder(Discord.IUser User, DateTimeOffset Time, string Message) {
            Reminder r = new() {
                ID = GenerateToken(),
                IssuerID = User.Id,
                DateTimeRelease = Time.ToUnixTimeSeconds(),
                Message = Message
            };

            Reminders.Add(r);
            SaveChanges();
            return r;
        }

        private int Count = 1;

        /// <summary>
        /// Generates the lowest available token above a local counter used for optimization.
        /// </summary>
        /// <returns>A unique integer that can be used as a key for a new Event in the Events database.</returns>

        public int GenerateToken() {
            while (Reminders.Find(Count) != null) Count++;
            return Count;
        }
    }
}
