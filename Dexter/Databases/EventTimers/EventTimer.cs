using System.ComponentModel.DataAnnotations;

namespace Dexter.Databases.EventTimers {

    /// <summary>
    /// Database-compatible class to deal with calling time-based or time-limited events when required.
    /// </summary>

    public class EventTimer {

        /// <summary>
        /// Unique identifier for the EventTimer object.
        /// </summary>

        [Key]
        public string Token { get; set; }
        
        /// <summary>
        /// The delta-time since the event is created until it expires, in seconds.
        /// </summary>

        public int ExpirationLength { get; set; }

        /// <summary>
        /// The Expiration Time field specifies the UNIX time the timer expires at.
        /// </summary>

        public long ExpirationTime { get; set; }

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
        /// Specifies whether the timer is an interval, is set to expire, or has expired.
        /// </summary>

        public TimerType TimerType { get; set; }

    }

}
