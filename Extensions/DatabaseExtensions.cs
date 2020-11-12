using Dexter.Databases.FunTopics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Extensions {

    /// <summary>
    /// The Database Extensions class offers a variety of different extensions that can be applied to a DiscordSocketClient.
    /// </summary>
    public static class DatabaseExtensions {

        /// <summary>
        /// The GetRandomTopic command extends upon a database set and returns a random, valid entry.
        /// </summary>
        /// <param name="Topics">The topics field is the set of fun topics you wish to query from.</param>
        /// <returns>A tasked result of an instance of a fun object.</returns>
        public static async Task<FunTopic> GetRandomTopic(this DbSet<FunTopic> Topics) {
            int RandomID = new Random().Next(await Topics.AsQueryable().CountAsync());
            FunTopic FunTopic = Topics.AsQueryable().Where(Topic => Topic.TopicID == RandomID && Topic.Type != TopicType.Disabled).FirstOrDefault();

            if (FunTopic != null)
                return FunTopic;
            else return await Topics.GetRandomTopic();
        }

    }
}
