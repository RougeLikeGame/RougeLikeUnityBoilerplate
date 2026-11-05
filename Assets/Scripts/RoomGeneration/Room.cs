using System.Collections.Generic;
using UnityEngine;
using Game.RoomGeneration;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Used by the RoomGenerator to visualize a room.
    /// </summary>
    public sealed class Room : MonoBehaviour
    {
        /// <summary>
        /// The color representation of each type.
        /// </summary>
        private static readonly Dictionary<RoomType, Color> RoomTypeColors = new()
        {
            [RoomType.Default] = Color.white,
            [RoomType.Start] = Color.yellow,
            [RoomType.End] = Color.red
        };

        /// <summary>
        /// Each type of room used for visualization.
        /// </summary>
        public enum RoomType
        {
            Default,
            Start,
            End
        }

        /// <summary>
        /// The walls of the room.
        /// </summary>
        [SerializeField]
        private List<SpriteRenderer> m_walls;

        /// <summary>
        /// The sprite of the room.
        /// </summary>
        private SpriteRenderer m_sprite;

        /// <summary>
        /// Modifies the room to visualize the cell of the RoomGenerator's grid.
        /// </summary>
        /// <param name="cell"></param>
        public void VisualizeCell(Prims.Cell cell, RoomType type = RoomType.Default)
        {
            m_sprite.color = RoomTypeColors[type];

            foreach (KeyValuePair<Prims.Cell.Direction, bool> pair in cell.Walls)
            {
                int index = (int)pair.Key;

                if (index > m_walls.Count)
                    continue;

                SpriteRenderer sprite = m_walls[index];

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