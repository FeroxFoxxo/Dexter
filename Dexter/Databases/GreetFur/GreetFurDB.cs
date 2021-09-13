using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.UserProfiles;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Databases.GreetFur
{

    /// <summary>
    /// An abstraction of the data structure holding all relevant information about user levels.
    /// </summary>

    public class GreetFurDB : Database
    {
        /// <summary>
        /// Holds relevant information about users' time zones and personal details relevant to recordkeeping.
        /// </summary>

        public ProfilesDB ProfilesDB { get; set; }

        /// <summary>
        /// Holds relevant configuration about time zone formatting.
        /// </summary>

        public LanguageConfiguration LanguageConfiguration { get; set; }

        /// <summary>
        /// The set of all records for days GreetFurs have been active.
        /// </summary>

        public DbSet<GreetFurRecord> Records { get; set; }

        /// <summary>
        /// Gets all recent activity starting from a given <paramref name="day"/>.
        /// </summary>
        /// <param name="day">The day to start pulling information from.</param>
        /// <param name="length">The length of the period to consider starting from <paramref name="day"/></param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> where the key holds all caught user IDs and the values are all their attached records sorted by date.</returns>

        public Dictionary<ulong, List<GreetFurRecord>> GetAllRecentActivity(int day, int length = 14)
        {
            List<GreetFurRecord> allEntries = Records.AsQueryable().Where(r => r.Date >= day && r.Date < day + length).ToList();

            Dictionary<ulong, List<GreetFurRecord>> result = new();

            foreach (GreetFurRecord r in allEntries)
            {
                if (result.ContainsKey(r.UserId))
                {
                    result[r.UserId].Add(r);
                } 
                else
                {
                    result.Add(r.UserId, new List<GreetFurRecord>() { r });
                }
            }

            foreach (List<GreetFurRecord> ls in result.Values)
            {
                ls.Sort((a, b) => a.Date.CompareTo(b.Date));
            }

            return result;
        }

        /// <summary>
        /// Gets a number of days of activity for a user from a given <paramref name="day"/> up to a given <paramref name="length"/>.
        /// </summary>
        /// <param name="greetFurId">The ID of the target user to query activity for.</param>
        /// <param name="day">The day to start pulling records from.</param>
        /// <param name="length">The length of the period to query for, in days.</param>
        /// <param name="fakeNullEntries">If <see langword="false"/>, entries not found in the database will be null as opposed to records with no messages.</param>
        /// <returns>An array of <see cref="GreetFurRecord"/> whose length equals <paramref name="length"/>. If no activity is logged for a user, it returns a record with no messages.</returns>

        public GreetFurRecord[] GetRecentActivity(ulong greetFurId, int day, int length = 14, bool fakeNullEntries = true)
        {
            GreetFurRecord[] result = new GreetFurRecord[length];
            for(int i = 0; i < length; i++)
            {
                GreetFurRecord r = GetActivity(greetFurId, day + i);
                if (r is null && fakeNullEntries)
                {
                    r = new GreetFurRecord() { RecordId = 0, UserId = greetFurId, Date = day + i, MessageCount = 0, MutedUser = false };
                }
                result[i] = r;
            }

            return result;
        }

        /// <summary>
        /// Gets the activity record for a specific greetFur on a specific day.
        /// </summary>
        /// <param name="greetFurId">The ID of the target GreetFur.</param>
        /// <param name="day">The day to query for (in days since UNIX time).</param>
        /// <returns>A GreetFurRecord containing all relevant information for the queried data.</returns>

        public GreetFurRecord GetActivity(ulong greetFurId, int day)
        {
            return Records.AsQueryable().Where(r => r.Date == day && r.UserId == greetFurId).FirstOrDefault();
        }

        /// <summary>
        /// Adds new activity to a target user for the day (or a specific day if <paramref name="date"/> is specified.)
        /// </summary>
        /// <param name="greetFurId">The ID of the target user to update.</param>
        /// <param name="increment">The amount of new messages to log.</param>
        /// <param name="mutedUser">Whether the user has muted another member.</param>
        /// <param name="date">The date and time to modify a record for.</param>
        /// <returns>The <see cref="GreetFurRecord"/> that was modified.</returns>

        public GreetFurRecord AddActivity(ulong greetFurId, int increment = 1, bool mutedUser = false, DateTimeOffset date = default)
        {
            if (date == default)
            {
                date = ProfilesDB.GetOrCreateProfile(greetFurId).GetNow(LanguageConfiguration);
            }
            int day = GetDayFromDate(date);

            GreetFurRecord r = GetActivity(greetFurId, day);
            if (r is null)
            {
                r = new()
                {
                    RecordId = 0,
                    UserId = greetFurId,
                    Date = day,
                    MessageCount = 0,
                    MutedUser = false
                };
                Records.Add(r);
            }

            r.MessageCount += increment;
            r.MutedUser |= mutedUser;

            SaveChanges();
            return r;
        }

        /// <summary>
        /// Gets the current day (in days since UNIX time) for a given user in the Profiles Database.
        /// </summary>
        /// <param name="id">The ID of the target user.</param>
        /// <returns>The number of days that have transcurred since UNIX time for the given user.</returns>

        public int GetDayForUser(ulong id)
        {
            DateTimeOffset date = ProfilesDB.GetOrCreateProfile(id).GetNow(LanguageConfiguration);
            return GetDayFromDate(date);
        }

        /// <summary>
        /// Gets the number of days since 1st of January, 1970 for the given user in their time zone.
        /// </summary>
        /// <param name="d">The date time offset representing the current time for the user.</param>
        /// <returns>The number of days transcurred.</returns>

        public static int GetDayFromDate(DateTimeOffset d)
        {
            return (int)((d + d.Offset).ToUnixTimeSeconds() / (60 * 60 * 24));
        }

    }
}
