namespace Dexter.Helpers
{

    /// <summary>
    /// Contains a few methods that allow for treatment of arrays similarly to lists, with a few limitations.
    /// </summary>

    public class ArrayHelper
    {

        /// <summary>
        /// Adds a non-default value to an array, and resizes it in a manner that keeps time complexity linear.
        /// </summary>
        /// <remarks>The method overwrites the following of the last non-default value in the array, if you're using an alternative default use <paramref name="overrideDefault"/>.</remarks>
        /// <typeparam name="T">The type the array holds.</typeparam>
        /// <param name="array">The array <paramref name="item"/> is to be appended to.</param>
        /// <param name="item">The item to append to <paramref name="array"/>.</param>
        /// <param name="overrideDefault">In case you're using a nonstandard default for your array (empty elements).</param>

        public static void AddLinear<T>(ref T[] array, T item, T overrideDefault = default)
        {
            if (!array[^1].Equals(overrideDefault))
            {
                int i = array.Length;
                System.Array.Resize(ref array, array.Length * 2);

                array[i] = item;
                return;
            }

            for (int i = array.Length - 1; i > 0; i--)
            {
                if (!array[i - 1].Equals(overrideDefault))
                {
                    array[i] = item;
                    return;
                }
            }
        }

        /// <summary>
        /// Removes the first occurrence of an element <paramref name="item"/> in an <paramref name="array"/>.
        /// </summary>
        /// <typeparam name="T">The type the array holds.</typeparam>
        /// <param name="array">The array to remove <paramref name="item"/> from.</param>
        /// <param name="item">The item to look for in <paramref name="array"/>, the first occurrence of which will be removed.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> was found, otherwise <see langword="false"/>.</returns>

        public static bool RemoveLinear<T>(ref T[] array, T item)
        {
            bool found = false;
            int lastNonDefault = 0;

            for (int i = 0; i < array.Length; i++)
            {
                if (!array[i].Equals(default)) lastNonDefault = i;

                if (found)
                {
                    array[i - 1] = array[i];
                }
                else
                {
                    if (array[i].Equals(item))
                    {
                        array[i] = default;
                        found = true;
                    }
                }
            }

            if (lastNonDefault < array.Length / 2)
            {
                System.Array.Resize(ref array, array.Length / 2);
            }

            return found;
        }

    }
}
