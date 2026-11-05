using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        /// The maximum amount of rooms that can be generated
        /// </summary>
        [SerializeField]
        private int m_maxDepth;

        /// <summary>
        /// The seed of the room generation.
        /// </summary>
        [SerializeField]
        private int m_seed;

        /// <summary>
        /// The prefab instantiated for each cell in the grid.
        /// </summary>
        [SerializeField]
        private Room m_room;

        /// <summary>
        /// Clears old rooms and regenerates new ones.
        /// </summary>
        public void RegenerateRooms()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            StopAllCoroutines();
            StartCoroutine(nameof(Generate));
        }

        /// <summary>
        /// Generates each room using Prim's Algorithm.
        /// </summary>
        /// <returns></returns>
        private IEnumerator Generate()
        {
            Prims maze = new Prims(m_gridSize);

            List<Prims.Cell> path = maze.Generate(m_maxDepth, m_seed);
            Prims.Cell start = path.First(), end = path.Last();

            foreach (Prims.Cell cell in path)
            {
                Room room = Instantiate(m_room, new(cell.Coordinate.x - m_gridSize / 2.0f, cell.Coordinate.y - m_gridSize / 2.0f), Quaternion.identity, transform);

                room.VisualizeCell(cell, cell == end ? Room.RoomType.End : cell == start ? Room.RoomType.Start : Room.RoomType.Default);

                yield return new WaitForSeconds(0.20f);
            }
        }
    }
}
