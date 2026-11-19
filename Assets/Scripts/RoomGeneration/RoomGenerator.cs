using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Generates a maze using Prim's Algorithm and visualizes it.
    /// </summary>
    public sealed class RoomGenerator : MonoBehaviour
    {
        /// <summary>
        /// Invoked when the room generator generates a new layout.
        /// </summary>
        public UnityEvent<List<RoomObject>, int> OnRoomsGenerated;

        /// <summary>
        /// The size of the grid.
        /// </summary>
        [Header("Generation Settings")]
        [SerializeField]
        private int m_gridSize;

        /// <summary>
        /// The seed of the room generation.
        /// </summary>
        [SerializeField]
        private int m_seed;
        
        /// <summary>
        /// The available rooms that can be randomly chosen and generated.
        /// </summary>
        [Header("Room Generation")]
        [SerializeField]
        private List<RoomData> m_availableRooms;

        /// <summary>
        /// The maximum amount of rooms that can be generated
        /// </summary>
        [SerializeField]
        private int m_maxRooms;

        [SerializeField]
        private int m_roomWidth;

        [SerializeField]
        private int m_roomHeight;

        /// <summary>
        /// Clears old rooms and regenerates new ones.
        /// </summary>
        public void RegenerateRooms()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            Generate();
        }

        /// <summary>
        /// Generates each room using Prim's Algorithm.
        /// </summary>
        /// <returns></returns>
        private void Generate()
        {
            uint weightSum = m_availableRooms.GetWeightSum();
            System.Random generator = new(m_seed);
            Prims maze = new Prims(m_gridSize);

            float halfGridSize = m_gridSize / 2.0f;
            float halfRoomWidth = m_roomWidth / 2.0f;
            float halfRoomHeight = m_roomHeight / 2.0f;

            List<RoomObject> rooms = new();

            foreach (Prims.Cell cell in maze.Generate(m_maxRooms, generator))
            {
                RoomData data = m_availableRooms.GetWeightedElement(generator, weightSum);
                RoomObject room = Instantiate(data.RoomObject, new(halfRoomWidth - m_roomWidth * (halfGridSize - cell.Coordinate.x), halfRoomHeight - m_roomHeight * (halfGridSize - cell.Coordinate.y)), Quaternion.identity, transform);

                room.Initialize(data, cell);

                rooms.Add(room);
            }

            OnRoomsGenerated.Invoke(rooms, m_gridSize);
        }
    }
}
