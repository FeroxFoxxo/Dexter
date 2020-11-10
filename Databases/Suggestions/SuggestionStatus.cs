namespace Dexter.Databases.Suggestions {

    /// <summary>
    /// This enum is to be used to clarify which point of contention the suggestion is in.
    /// It specifies whether it has been suggested in the specified channel, if the suggestion
    /// is pending and has been passed through, if the suggestion has been approved or denied,
    /// or if the suggestion has since been deleted.
    /// </summary>
    public enum SuggestionStatus {

        /// <summary>
        /// The SUGGESTED field specifies that the suggestion has been proposed and the
        /// community voting process on it is currently active.
        /// </summary>
        Suggested,

        /// <summary>
        /// The PENDING field specifies that the suggestion has passed the community voting
        /// stage and is now in the staff voting stage, as will be voted on through the staff. 
        /// (IE it has reached the threshold of upvotes over downvotes to have been declared wanted)
        /// </summary>
        Pending,
        
        /// <summary>
        /// The APPROVED field specifies that the suggestion has been approved by a moderator through the related approval command.
        /// </summary>
        Approved,

        /// <summary>
        /// The DECLINED field specifies that the suggestion has either been pass through staff
        /// voting but has since been denied by a moderator or has been declined by the community
        /// (IE it has reached the threshold of downvotes over upvotes to have been declared unwanted)
        /// </summary>
        Declined,

        /// <summary>
        /// The DELETED field specifies that a user has purposely deleted a suggestion through the trashcan
        /// reaction situated in the suggestion. This can only be done by the author of the suggestion.
        /// </summary>
        Deleted

    }
}
