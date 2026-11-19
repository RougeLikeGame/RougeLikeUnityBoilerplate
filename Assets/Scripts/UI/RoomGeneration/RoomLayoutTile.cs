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
        /// The color applied to the tile when it is highlighted.
        /// </summary>
        [Header("Tile Settings")]
        [SerializeField]
        private Color m_highlightColor;

        /// <summary>
        /// The color applied to the tile when it is not highlighted.
        /// </summary>
        [SerializeField]
        private Color m_baseColor;

        /// <summary>
        /// The image of the tile.
        /// </summary>
        private Image m_image;

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
            m_image.color = enabled ? m_highlightColor : m_baseColor;
        }

        private void Awake()
        {
            m_image = GetComponent<Image>();
            m_image.color = m_baseColor;
        }
    }
}
