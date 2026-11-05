using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Generates a maze using Prim's Algorithm and visualizes it.
    /// </summary>
    public sealed class RoomGenerator : MonoBehaviour
    {
        /// <summary>
        /// The size of the grid.
        /// </summary>
        [SerializeField]
        private int m_gridSize;

        /// <summary>
        /// How many rooms of the maze.
        /// </summary>
        [SerializeField]
        private int m_maxDepth;

        /// <summary>
        /// The prefab instantiated for each cell in the grid.
        /// </summary>
        [SerializeField]
        private Room m_room;

        private void Start()
        {
            StartCoroutine(nameof(Generate));
        }

        /// <summary>
        /// Generates each room using Prim's Algorithm.
        /// </summary>
        /// <returns></returns>
        private IEnumerator Generate()
        {
            Prims maze = new Prims(m_gridSize);
            foreach (Prims.Cell cell in maze.Generate(m_maxDepth))
            {
                Room room = Instantiate(m_room, new(cell.Coordinate.x - m_gridSize / 2.0f, cell.Coordinate.y - m_gridSize / 2.0f), Quaternion.identity, transform);

                room.VisualizeCell(cell);

                yield return null;
            }
        }
    }
}
