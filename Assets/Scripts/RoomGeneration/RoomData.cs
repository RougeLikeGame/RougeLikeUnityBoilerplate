using UnityEngine;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Metadata for a room.
    /// </summary>
    [CreateAssetMenu(fileName = "Room", menuName = "ScriptableObjects/Room")]
    public sealed class RoomData : ScriptableObject, IWeightedElement
    {
        /// <summary>
        /// The chance of the room generating.
        /// </summary>
        [field: SerializeField]
        public uint Weight { get; set; }

        /// <summary>
        /// The room to instantiate when generated.
        /// </summary>
        [field: SerializeField]
        public RoomObject RoomObject { get; private set; }

        /// <summary>
        /// Whether or not the room generates once in generation.
        /// </summary>
        [field: SerializeField]
        public bool GenerateOnce { get; private set; }
    }
}
