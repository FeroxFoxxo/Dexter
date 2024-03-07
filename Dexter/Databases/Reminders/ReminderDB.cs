using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Dexter.Databases.Reminders
{

    /// <summary>
    /// Contains all reminders and relevant management methods for the Reminder System.
    /// </summary>

    public class ReminderDB : Database
    {

        /// <summary>
        /// Holds all individual reminders and their related information for processing.
        /// </summary>

        public DbSet<Reminder> Reminders { get; set; }

        /// <summary>
        /// Gets a reminder from the Database by its <paramref name="reminderID"/>.
        /// </summary>
        /// <param name="reminderID">The ID of the reminder to fetch.</param>
        /// <returns>A <c>Reminder</c> object if one with the given ID exists, <see langword="null"/> otherwise.</returns>

        public Reminder GetReminder(int reminderID)
        {
            return Reminders.Find(reminderID);
        }

        /// <summary>
        /// Gets a list of all reminders filtered by IssuerID, where the Issuer is <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The Issuer of reminders to filter for.</param>
        /// <returns>A <c>Reminder[]</c> array, where all Issuers are <paramref name="user"/>.</returns>

        public Reminder[] GetRemindersByUser(Discord.IUser user)
        {
            return GetRemindersByUser(user.Id);
        }

        /// <summary>
        /// Gets a list of all reminders filtered by IssuerID, where the IssuerID is <paramref name="userID"/>.
        /// </summary>
        /// <param name="userID">The ID of the Issuer of reminders to filter for.</param>
        /// <returns>A <c>Reminder[]</c> array, where all IssuerIDs are <paramref name="userID"/>.</returns>

        public Reminder[] GetRemindersByUser(ulong userID)
        {
            return [.. Reminders.AsQueryable().Where(r => r.IssuerID == userID)];
        }

        /// <summary>
        /// Creates a new reminder from given parameters, adds it to the database, and returns it.
        /// </summary>
        /// <param name="user">The Issuer of the reminder.</param>
        /// <param name="time">The time the reminder should release at.</param>
        /// <param name="message">The content of the reminder.</param>
        /// <returns>A <c>Reminder</c> object which includes the assigned ID.</returns>

        public Reminder AddReminder(Discord.IUser user, DateTimeOffset time, string message)
        {
            Reminder r = new()
            {
                ID = GenerateToken(),
                IssuerID = user.Id,
                DateTimeRelease = time.ToUnixTimeSeconds(),
                Message = message
            };

            Reminders.Add(r);

            return r;
        }

        private int Count = 1;

        /// <summary>
        /// Generates the lowest available token above a local counter used for optimization.
        /// </summary>
        /// <returns>A unique integer that can be used as a key for a new Event in the Events database.</returns>

        public int GenerateToken()
        {
            while (Reminders.Find(Count) != null)
            {
                Count++;
            }

            return Count;
        }
    }
}
