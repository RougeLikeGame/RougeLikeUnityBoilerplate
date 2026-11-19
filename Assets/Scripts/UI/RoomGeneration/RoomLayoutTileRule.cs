using System.Collections.Generic;
using UnityEngine;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Rules for a room layout tile. If a generated room matches with the rules, the visuals of the room will be reflected on it's tile.
    /// </summary>
    public sealed class RoomLayoutTileRule : ScriptableObject
    {
        /// <summary>
        /// The sprite applied to the tile.
        /// </summary>
        [field: Header("Tile Visuals")]
        [field: SerializeField]
        public Sprite Sprite { get; private set; }

        /// <summary>
        /// The rotation applied to the sprite.
        /// </summary>
        [field: SerializeField]
        [field: Range(-180.0f, 180.0f)]
        public float Rotation { get; private set; }

        /// <summary>
        /// The required wall states for the rule to be valid.
        /// </summary>
        [field: Header("Tile Rules")]
        [field: SerializeField]
        public Dictionary<Prims.Cell.WallDirection, bool> RequiredWalls { get; private set; }
    }
}
