namespace Dexter.Databases.FunTopics {

    /// <summary>
    /// [risk of deprecation] The subtype a topic falls into. Can be of type TOPIC or WOULDYOURATHER.
    /// </summary>

    public enum TopicType {
        
        /// <summary>
        /// A topic that initiates a random, unprompted conversation, generally by asking an open question.
        /// </summary>

        Topic,

        /// <summary>
        /// A topic that initiates a conversation by proposing two options and opening up a binary choice.
        /// </summary>

        WouldYouRather

    }

}
