using System;

namespace Dexter.Abstractions {

    public class CooldownException : Exception {

        public TimeSpan CooldownTime { get; private set; }

        public CooldownException(TimeSpan CooldownTime) {
            this.CooldownTime = CooldownTime;
        }

    }

}
