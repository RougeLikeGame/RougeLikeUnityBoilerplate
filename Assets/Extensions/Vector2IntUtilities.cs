using UnityEngine;

namespace UnityEngine
{
    /// <summary>
    /// Extension methods for the Vector2Int class.
    /// </summary>
    public static class Vector2IntUtilities 
    {
        /// <summary>
        /// Returns a normalized copy of the vector.
        /// </summary>
        /// <param name="v"></param>
        /// <returns>The normalized copy of the vector.</returns>
        public static Vector2Int Normalized(this Vector2Int v)
        {
            Vector2 floatV = new Vector2(v.x, v.y);
            return Vector2Int.RoundToInt(floatV.normalized);
        }
    }
}
