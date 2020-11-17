namespace Dexter.Databases.FunTopics {

    /// <summary>
    /// The TopicType specifies whether the topic has been enabled or disabled.
    /// </summary>
    public enum TopicType {

        /// <summary>
        /// An enabled topic will have a chance to be displayed on run of the topic command.
        /// </summary>
        Enabled,

        /// <summary>
        /// A disabled topic will not run when the topic command runs. It will simply issue a new random topic.
        /// </summary>
        Disabled

    }
}
