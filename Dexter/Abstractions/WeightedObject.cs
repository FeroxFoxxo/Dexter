using System.Collections.Generic;

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
		public T Obj;

		/// <summary>
		/// The weight attached to this object.
		/// </summary>
		public double Weight;

		/// <summary>
		/// Creates a new WeightedObject given a base object and its weight.
		/// </summary>
		/// <param name="obj">The object to hold.</param>
		/// <param name="weight">The weight of the object.</param>

		public WeightedObject(T obj = default, double weight = 0)
		{
			Obj = obj;
			Weight = weight;
		}

		/// <summary>
		/// Sorts a given list of weighted objects <paramref name="objs"/> in a given order by weight.
		/// </summary>
		/// <param name="objs">The list to sort.</param>
		/// <param name="descending">Whether to sort the list in a descending order.</param>

		public static void SortByWeightInPlace(List<WeightedObject<T>> objs, bool descending = false)
		{
			objs.Sort((a, b) => (a.Weight < b.Weight ^ descending) ? -1 : 1);
		}
	}
}
