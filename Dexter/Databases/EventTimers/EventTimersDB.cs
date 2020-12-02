using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.EventTimers {

    public class EventTimersDB : EntityDatabase {

        public DbSet<EventTimer> EventTimers { get; set; }

    }

}
