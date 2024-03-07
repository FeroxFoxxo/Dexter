namespace Dexter.Databases.Games
{
    /// <summary>
    /// Represents an integer that has a set range from which it can't deviate.
    /// </summary>

    public struct BoundedInt
    {
        /// <summary>
        /// The minimum permitted value for the integer
        /// </summary>
        public int min;
        /// <summary>
        /// The maximum permitted value for the integer
        /// </summary>
        public int max;
        private int value;

        /// <summary>
        /// The value this integer represents
        /// </summary>
        public int Value
        {
            get
            {
                return value;
            }
            set
            {
                if (value > max)
                {
                    this.value = max;
                }
                else if (value < min)
                {
                    this.value = min;
                }
                else
                {
                    this.value = value;
                }
            }
        }

        /// <summary>
        /// Implicit conversion to an integer.
        /// </summary>
        /// <param name="b">The bounded integer to convert.</param>
        public static implicit operator int(BoundedInt b) => b.Value;
    }
}
