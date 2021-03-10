using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.AdminConfirmations {

    /// <summary>
    /// The AdminConfirmation class contains information on a topic needed for approval by the admins,
    /// including an automatically generated alphanumeric tracker as its key, the class and method it
    /// will callback to, and the parameters it will callback with.
    /// </summary>
    
    public class AdminConfirmation {

        /// <summary>
        /// The TrackerID field is the KEY of the table. It is unique per suggestion.
        /// It is an alphanumeric, 8 character long token that is randomly generated.
        /// </summary>
        
        [Key]

        public string Tracker { get; set; }

        /// <summary>
        /// The Callback Class field specifies the class in which the method is that will be called back to.
        /// </summary>
        
        public string CallbackClass { get; set; }

        /// <summary>
        /// The Callback Method field specifies the method that will be called if the confirmation is approved.
        /// </summary>
        
        public string CallbackMethod { get; set; }

        /// <summary>
        /// The Callback Parameters field specifies the parameters that will be called back to the method once approved.
        /// </summary>

        public string CallbackParameters { get; set; }

        /// <summary>
        /// Specifies the class of the callback method to be called if the proposal is denied.
        /// </summary>

        public string DenyCallbackClass { get; set; }

        /// <summary>
        /// Specifies the name of the method to be called if the proposal is denied.
        /// </summary>

        public string DenyCallbackMethod { get; set; }

        /// <summary>
        /// Specifies the parameters field to call the DenyCallbackMethod with if the proposal is denied.
        /// </summary>

        public string DenyCallbackParameters { get; set; }

    }

}
