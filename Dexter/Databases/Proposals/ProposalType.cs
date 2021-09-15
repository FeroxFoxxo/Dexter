namespace Dexter.Databases.Proposals
{

    /// <summary>
    /// This enum is used to clarify what type of proposal the suggested content is.
    /// </summary>

    public enum ProposalType
    {

        /// <summary>
        /// The SUGGESTION field specifies this is a proposal put fourth in the #suggestions channel.
        /// These are put fourth by the community to be voted on and are added automatically.
        /// </summary>

        Suggestion,

        /// <summary>
        /// The ADMIN CONFIRMATION field specifies this is a proposal run by a command, such as the
        /// custom commands, that will automatically run a command once approved.
        /// </summary>

        AdminConfirmation

    }
}
