using System.Collections.Generic;
using UnityEngine;
using Game.RoomGeneration;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Used by the RoomGenerator to visualize a room.
    /// </summary>
    public sealed class RoomObject : MonoBehaviour
    {
        /// <summary>
        /// The walls of the room.
        /// </summary>
        [SerializeField]
        private Dictionary<Prims.Cell.WallDirection, SpriteRenderer> m_walls;

        /// <summary>
        /// The sprite of the room.
        /// </summary>
        private SpriteRenderer m_sprite;

        /// <summary>
        /// Modifies the room to visualize the cell of the RoomGenerator's grid.
        /// </summary>
        /// <param name="cell"></param>
        public void Initialize(Prims.Cell cell)
        {
            foreach (KeyValuePair<Prims.Cell.WallDirection, bool> pair in cell.Walls)
            {
                SpriteRenderer sprite;

                if (!m_walls.TryGetValue(pair.Key, out sprite))
                    continue;

                if (pair.Value)
                    sprite.color = Color.black;
                else
                    Destroy(sprite.gameObject);
            }
        }

        private void Awake()
        {
            m_sprite = GetComponent<SpriteRenderer>();
        }
    }
}