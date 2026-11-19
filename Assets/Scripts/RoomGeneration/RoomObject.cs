using System.Collections.Generic;
using UnityEngine;
using Game.RoomGeneration;
using UnityEngine.Events;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Used by the RoomGenerator to visualize a room.
    /// </summary>
    public sealed class RoomObject : MonoBehaviour
    {
        /// <summary>
        /// Invoked when the player enters the room.
        /// </summary>
        public UnityEvent<RoomObject> OnPlayerEnter;

        /// <summary>
        /// Invoked when the player exits the room.
        /// </summary>
        public UnityEvent<RoomObject> OnPlayerExit;

        /// <summary>
        /// The room data used to generate the room.
        /// </summary>
        public RoomData Data { get; private set; }

        /// <summary>
        /// The cell used to generate the room.
        /// </summary>
        public Prims.Cell GenerationCell { get; private set; }

        /// <summary>
        /// The walls of the room.
        /// </summary>
        [Header("Generation")]
        [SerializeField]
        private Dictionary<Prims.Cell.WallDirection, SpriteRenderer> m_walls;

        /// <summary>
        /// Modifies the room to visualize the cell of the RoomGenerator's grid.
        /// </summary>
        /// <param name="cell"></param>
        public void Initialize(RoomData data, Prims.Cell cell)
        {
            foreach (KeyValuePair<Prims.Cell.WallDirection, bool> pair in cell.Walls)
            {
                SpriteRenderer sprite;

                if (!m_walls.TryGetValue(pair.Key, out sprite))
                    continue;

                if (pair.Value)
                    sprite.color = Color.black;
                else
                    sprite.gameObject.SetActive(false);
            }

            GenerationCell = cell;
            Data = data;
        }
    }
}