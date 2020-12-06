using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.EventTimers {

    public class EventTimersDB : Database {

        public DbSet<EventTimer> EventTimers { get; set; }

    }

}
