using Game.RoomGeneration;
using System.Diagnostics;
using System.Linq;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Interface for adding a weight to a class.
    /// </summary>
    public interface IWeightedElement
    {
        public uint Weight { get; set; }
    }
}

namespace System.Collections.Generic
{
    public static partial class ListUtilities
    {
        /// <summary>
        /// Returns the sum of all the weights in a list of weighted elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>The sum of the weight of all elements.</returns>
        public static uint GetWeightSum<T>(this List<T> list) where T : IWeightedElement
        {
            uint weight = 0;

            foreach (IWeightedElement element in list)
                weight += element.Weight;

            return weight;
        }

        /// <summary>
        /// Returns a random weighted element from a list of weighted elements. Uses a custom Random generator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="generator"></param>
        /// <param name="weightSum"></param>
        /// <returns>The random weighted element.</returns>
        public static T GetWeightedElement<T>(this List<T> list, Random generator, uint weightSum) where T : IWeightedElement
        {
            Debug.Assert(list.Count > 0, "Error - Unable to get a random weighted element from an empty list!");

            uint randomWeight = (uint)generator.Next(0, (int)weightSum);

            foreach (T element in list)
            {
                if (randomWeight < element.Weight)
                    return element;

                randomWeight -= element.Weight;
            }

            return list.First();
        }
    }
}