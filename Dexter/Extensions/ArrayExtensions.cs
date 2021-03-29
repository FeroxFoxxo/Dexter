namespace Dexter.Extensions {

    /// <summary>
    /// Contains a few methods that allow for treatment of arrays similarly to lists, with a few limitations.
    /// </summary>

    public static class ArrayExtensions {

        /// <summary>
        /// Adds a non-default value to an array, and resizes it in a manner that keeps time complexity linear.
        /// </summary>
        /// <remarks>The method overwrites the following of the last non-default value in the array, if you're using an alternative default use <paramref name="OverrideDefault"/>.</remarks>
        /// <typeparam name="T">The type the array holds.</typeparam>
        /// <param name="Array">The array <paramref name="Item"/> is to be appended to.</param>
        /// <param name="Item">The item to append to <paramref name="Array"/>.</param>
        /// <param name="OverrideDefault">In case you're using a nonstandard default for your array (empty elements).</param>
        /// <returns>The resized array (if necessary), the method modifies the original array if resizing isn't required.</returns>

        public static T[] AddLinear<T>(this T[] Array, T Item, T OverrideDefault = default) {
            if (!Array[^1].Equals(OverrideDefault)) {
                int i = Array.Length;
                System.Array.Resize(ref Array, Array.Length * 2);

                Array[i] = Item;
                return Array;
            }

            for (int i = Array.Length - 1; i > 0; i--) {
                if (!Array[i - 1].Equals(OverrideDefault)) {
                    Array[i] = Item;
                    return Array;
                }
            }

            Array[0] = Item;
            return Array;
        }

        /// <summary>
        /// Removes the first occurrence of an element <paramref name="Item"/> in an <paramref name="Array"/>.
        /// </summary>
        /// <typeparam name="T">The type the array holds.</typeparam>
        /// <param name="Array">The array to remove <paramref name="Item"/> from.</param>
        /// <param name="Item">The item to look for in <paramref name="Array"/>, the first occurrence of which will be removed.</param>
        /// <returns><see langword="true"/> if <paramref name="Item"/> was found, otherwise <see langword="false"/>.</returns>
        /// <returns>An array where <paramref name="Item"/> has been removed, resized to save space if its length is too high compared to its count.</returns>

        public static T[] RemoveLinear<T>(this T[] Array, T Item) {
            bool Found = false;
            int LastNonDefault = 0;

            for (int i = 0; i < Array.Length; i++) {
                if (!Array[i].Equals(default)) LastNonDefault = i;

                if (Found) {
                    Array[i - 1] = Array[i];
                }
                else {
                    if (Array[i].Equals(Item)) {
                        Array[i] = default;
                        Found = true;
                    }
                }
            }

            if (LastNonDefault < Array.Length / 2) {
                System.Array.Resize(ref Array, Array.Length / 2);
            }

            return Array;
        }

    }
}
