using UnityEngine;
using System.Collections.Generic;

namespace System.Collections.Generic
{
    /// <summary>
    /// Extension methods for the generic List class.
    /// </summary>
    public static partial class ListUtilities
    {
        /// <summary>
        /// Returns whether or not the list is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>Whether the list is empty.</returns>
        public static bool IsEmpty<T>(this List<T> list)
        {
            return list.Count <= 0;
        }

        /// <summary>
        /// Returns a random element in a list. Uses Unity's Random generator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>The random element in the list.</returns>
        public static T GetRandomElement<T>(this List<T> list)
        {
            Debug.Assert(list.Count > 0, "Error - Unable to get a random element from an empty list!");
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Returns a random element in a list. Uses a custom Random generator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="generator"></param>
        /// <returns>The random element in the list.</returns>
        public static T GetRandomElement<T>(this List<T> list, System.Random generator)
        {
            Debug.Assert(list.Count > 0, "Error - Unable to get a random element from an empty list!");
            return list[generator.Next(0, list.Count)];
        }

        /// <summary>
        /// Removes and returns a random element in a list. Uses Unity's Random generator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>The random element in the list</returns>
        public static T PopRandomElement<T>(this List<T> list)
        {
            Debug.Assert(list.Count > 0, "Error - Unable to pop a random element from an empty list!");
            
            int index = UnityEngine.Random.Range(0, list.Count);
            T element = list[index];
            list.RemoveAt(index);

            return element;
        }

        /// <summary>
        /// Removes and returns a random element in a list. Uses a custom Random generator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="generator"></param>
        /// <returns>The random element in the list.</returns>
        public static T PopRandomElement<T>(this List<T> list, System.Random generator)
        {
            Debug.Assert(list.Count > 0, "Error - Unable to pop a random element from an empty list!");

            int index = generator.Next(0, list.Count);
            T element = list[index];
            list.RemoveAt(index);

            return element;
        }
    }
}
