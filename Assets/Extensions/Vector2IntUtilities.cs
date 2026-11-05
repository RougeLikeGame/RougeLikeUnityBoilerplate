using UnityEngine;

namespace UnityEngine
{
    public static class Vector2IntUtilities 
    {
        public static Vector2Int Normalized(this Vector2Int v)
        {
            Vector2 floatV = new Vector2(v.x, v.y);
            return Vector2Int.RoundToInt(floatV.normalized);
        }
    }
}
