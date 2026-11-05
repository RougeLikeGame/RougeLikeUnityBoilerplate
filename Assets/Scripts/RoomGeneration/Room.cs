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
        /// The walls of the room.
        /// </summary>
        [SerializeField]
        private List<GameObject> m_walls;

        /// <summary>
        /// Modifies the room to visualize the cell of the RoomGenerator's grid.
        /// </summary>
        /// <param name="cell"></param>
        public void VisualizeCell(Prims.Cell cell)
        {
            foreach (KeyValuePair<Prims.Cell.Direction, bool> pair in cell.Walls)
            {
                if (pair.Value)
                    continue;

                int index = (int)pair.Key;

                if (index > m_walls.Count)
                    continue;

                Destroy(m_walls[index].gameObject);
            }
        }
    }
}