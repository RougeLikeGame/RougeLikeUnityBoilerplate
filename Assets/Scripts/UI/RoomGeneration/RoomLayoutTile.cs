using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.RoomGeneration
{
    /// <summary>
    /// A tile for the room layout map. Each tile represents a room.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class RoomLayoutTile : MonoBehaviour
    {
        /// <summary>
        /// All available states of a room tile.
        /// </summary>
        private enum TileState
        {
            Hidden,
            Visible,
            Highlighted
        }

        /// <summary>
        /// The color applied to the tile when it is highlighted.
        /// </summary>
        [field: Header("Tile Settings")]
        [field: SerializeField]
        private Dictionary<TileState, Color> m_stateColors = new();

        /// <summary>
        /// The image of the tile.
        /// </summary>
        private Image m_image;

        /// <summary>
        /// The state of the tile.
        /// </summary>
        private TileState m_state;

        /// <summary>
        /// Initializes the tile based on the tile rule.
        /// </summary>
        /// <param name="rule"></param>
        public void Initialize(RoomLayoutTileRule rule)
        {
            m_image.sprite = rule.Sprite;
            transform.rotation = Quaternion.Euler(Vector3.forward * rule.Rotation);
        }

        /// <summary>
        /// Highlights the tile, making it more visible on the map.
        /// </summary>
        /// <param name="enabled"></param>
        public void Highlight(bool enabled)
        {
            m_state = enabled ? TileState.Highlighted : TileState.Visible;
            m_image.color = m_stateColors.GetValueOrDefault(m_state);
        }

        private void Awake()
        {
            m_image = GetComponent<Image>();
            m_image.color = m_stateColors.GetValueOrDefault(m_state);
        }
    }
}
