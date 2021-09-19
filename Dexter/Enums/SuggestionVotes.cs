namespace Dexter.Enums
{

    /// <summary>
    /// This enum is to be used to tell whether a suggestion has reached a passing
    /// or failing threshold, or if the suggestion has still been undecided on.
    /// </summary>

    public enum SuggestionVotes
    {

        /// <summary>
        /// The PASS field specifies that the suggestion has reached the threshold of
        /// upvotes over downvotes to be passed to the staff suggestions channel.
        /// </summary>

        Pass,

        /// <summary>
        /// The FAIL field specifies that the suggestion has reached the threshold of
        /// being declined by the community, and will not pass it to the staff suggestions.
        /// </summary>

        Fail,

        /// <summary>
        /// The REMAIN field specifies that the suggestion has still been undecided on,
        /// and subsequently voting on it will be continued until it has been or it expires.
        /// </summary>

        Remain

    }

}
