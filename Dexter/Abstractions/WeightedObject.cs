using System.Collections.Generic;
using System.Linq;
namespace Dexter.Abstractions
{

    /// <summary>
    /// A structure containing an object and a related value used for sorting.
    /// </summary>

    public class WeightedObject<T>
    {
        /// <summary>
        /// The object held in this instance.
        /// </summary>
        public T obj;
        /// <summary>
        /// The weight attached to this object.
        /// </summary>
        public double weight;

        /// <summary>
        /// Creates a new WeightedObject given a base object and its weight.
        /// </summary>
        /// <param name="obj">The object to hold.</param>
        /// <param name="weight">The weight of the object.</param>

        public WeightedObject(T obj = default, double weight = 0)
        {
            this.obj = obj;
            this.weight = weight;
        }

        /// <summary>
        /// Sorts an enumerable collection of WeightedObjects by their weight.
        /// </summary>
        /// <param name="objs">The collection of objects.</param>
        /// <param name="descending">Whether to sort in descending order.</param>
        /// <returns>A list with the values in <paramref name="objs"/> sorted in ascending order if <paramref name="descending"/> is <see langword="false"/>, or otherwise in descending order.</returns>

        public static List<WeightedObject<T>> SortByWeight(IEnumerable<WeightedObject<T>> objs, bool descending = false)
        {
            List<WeightedObject<T>> result = objs.ToList();

            SortByWeightInPlace(result);
            return result;
        }

        /// <summary>
        /// Sorts a given list of weighted objects <paramref name="objs"/> in a given order by weight.
        /// </summary>
        /// <param name="objs">The list to sort.</param>
        /// <param name="descending">Whether to sort the list in a descending order.</param>

        public static void SortByWeightInPlace(List<WeightedObject<T>> objs, bool descending = false)
        {
            objs.Sort((a, b) => (a.weight < b.weight ^ descending) ? -1 : 1);
        }
    }
}
