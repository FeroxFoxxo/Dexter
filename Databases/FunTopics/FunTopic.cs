using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.FunTopics {

    /// <summary>
    /// The FunTopic class contains information on a topic, including the suggestor's ID and the question proposed.
    /// </summary>
    public class FunTopic {

        /// <summary>
        /// The TOPIC ID field is the KEY of the database. It is what is used to delete the topic from the database.
        /// </summary>
        [Key]
        public int TopicID { get; set; }

        /// <summary>
        /// The PROPOSER ID field specifies the person who has proposed the topic to the bot.
        /// </summary>
        public ulong ProposerID { get; set; }

        /// <summary>
        /// The TOPIC field specifies the topic that will be printed once the command has run.
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// A TopicType specifies whether the desired topic is enabled to be shown.
        /// </summary>
        public TopicType TopicType { get; set; }

    }

}
