using UnityEngine;
using System.Collections.Generic;

namespace System.Collections.Generic
{
    public static class ListUtilities
    {
        public static bool IsEmpty<T>(this List<T> list)
        {
            return list.Count <= 0;
        }

        public static T GetRandomElement<T>(this List<T> list)
        {
            Debug.Assert(list.Count > 0, "Error - Unable to get a random element from an empty list!");
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static T PopRandomElement<T>(this List<T> list)
        {
            Debug.Assert(list.Count > 0, "Error - Unable to pop a random element from an empty list!");
            
            int index = UnityEngine.Random.Range(0, list.Count);
            T element = list[index];
            list.RemoveAt(index);

            return element;
        }
    }
}
