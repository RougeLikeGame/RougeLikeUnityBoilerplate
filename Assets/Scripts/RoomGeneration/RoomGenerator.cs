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
        [Header("Settings")]
        [SerializeField]
        private int m_gridSize;

        /// <summary>
        /// The maximum amount of rooms that can be generated
        /// </summary>
        [SerializeField]
        private int m_maxRooms;

        /// <summary>
        /// The seed of the room generation.
        /// </summary>
        [SerializeField]
        private int m_seed;
        
        /// <summary>
        /// The available rooms that can be randomly chosen and generated.
        /// </summary>
        [Header("Generation")]
        [SerializeField]
        private List<RoomData> m_availableRooms;

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
            uint weightSum = m_availableRooms.GetWeightSum();
            System.Random generator = new(m_seed);
            Prims maze = new Prims(m_gridSize);

            foreach (Prims.Cell cell in maze.Generate(m_maxRooms, generator))
            {
                RoomData randomRoom = m_availableRooms.GetWeightedElement(generator, weightSum);
                RoomObject room = Instantiate(randomRoom.RoomObject, new(cell.Coordinate.x - m_gridSize / 2.0f, cell.Coordinate.y - m_gridSize / 2.0f), Quaternion.identity, transform);

                room.Initialize(cell);

                yield return new WaitForSeconds(0.20f);
            }
        }
    }
}
