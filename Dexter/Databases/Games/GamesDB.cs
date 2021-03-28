using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dexter.Databases.Games {

    public class GamesDB : Database {

        public DbSet<GameInstance> Games { get; set; }

        public DbSet<Player> Players { get; set; }

    }
}
