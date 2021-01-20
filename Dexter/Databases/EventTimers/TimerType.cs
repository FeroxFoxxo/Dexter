namespace Dexter.Databases.EventTimers {

    /// <summary>
    /// Classification of event timers. An event timer may have a TimerType of INTERVAL, EXPIRE, or EXPIRED.
    /// </summary>

    public enum TimerType {

        /// <summary>
        /// Indicates that the timer is to be run periodically every so often, it doesn't expire.
        /// </summary>

        Interval,

        /// <summary>
        /// Indicates that the timer is set to expire at some point in the future, but is currently active.
        /// </summary>

        Expire

    }

}
