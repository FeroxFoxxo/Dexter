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
        /// Gets a number of days of activity for a user from a given <paramref name="firstDay"/> up to a given <paramref name="length"/>.
        /// </summary>
        /// <param name="greetFurId">The ID of the target user to query activity for.</param>
        /// <param name="day">The day to start pulling records from.</param>
        /// <param name="length">The length of the period to query for, in days.</param>
        /// <returns>An array of <see cref="GreetFurRecord"/> whose length equals <paramref name="length"/>. If no activity is logged for a user, it returns a record with no messages.</returns>

        public GreetFurRecord[] GetRecentActivity(ulong greetFurId, int day, int length = 14)
        {
            GreetFurRecord[] result = new GreetFurRecord[length];
            for(int i = 0; i < length; i++)
            {
                GreetFurRecord r = GetActivity(greetFurId, day + i);
                if (r is null)
                {
                    r = new GreetFurRecord() { RecordId = 0, UserID = greetFurId, Date = day + i, MessageCount = 0, MutedUser = false };
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
            return Records.AsQueryable().Where(r => r.Date == day && r.UserID == greetFurId).FirstOrDefault();
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
            int day = (int) (date.ToUnixTimeSeconds() / (60 * 60 * 24));

            GreetFurRecord r = GetActivity(greetFurId, day);
            if (r is null)
                Records.Add(new()
                {
                    UserID = greetFurId,
                    Date = day,
                    MessageCount = 0,
                    MutedUser = false
                });

            r.MessageCount += increment;
            r.MutedUser |= mutedUser;

            SaveChanges();
            return r;
        }

    }
}
