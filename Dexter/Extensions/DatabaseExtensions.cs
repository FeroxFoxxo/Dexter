using Dexter.Databases.FunTopics;
using Dexter.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Extensions {

    /// <summary>
    /// The Database Extensions class offers a variety of different extensions that can be applied to a database.
    /// </summary>
    
    public static class DatabaseExtensions {

        /// <summary>
        /// The GetRandomTopic command extends upon a database set and returns a random, valid entry.
        /// </summary>
        /// <param name="Topics">The topics field is the set of fun topics you wish to query from.</param>
        /// <param name="TopicType">The type of topic to draw from. It may be a TOPIC or a WOULDYOURATHER.</param>
        /// <returns>A tasked result of an instance of a fun object.</returns>
        
        public static FunTopic GetRandomTopic(this DbSet<FunTopic> Topics, TopicType TopicType) {
            FunTopic[] eligible = Topics.AsQueryable().Where(t => t.TopicType == TopicType && t.EntryType == EntryType.Issue).ToArray();
            
            if (!eligible.Any())
                return null;

            int randomID = new Random().Next(0, eligible.Length);

            return eligible[randomID];
        }

    }

}
