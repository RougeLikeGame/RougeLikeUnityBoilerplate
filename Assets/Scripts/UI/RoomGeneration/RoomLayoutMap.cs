using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Game.RoomGeneration.Prims;

namespace Game.RoomGeneration
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class RoomLayoutMap : MonoBehaviour
    {
        /// <summary>
        /// All available rules to be check against each generated room.
        /// </summary>
        [SerializeField]
        private List<RoomLayoutTileRule> m_rules;

        /// <summary>
        /// The size of each tile.
        /// </summary>
        [SerializeField]
        private int m_tileSize;

        /// <summary>
        /// The tile prefab to clone for each generated room.
        /// </summary>
        [SerializeField]
        private RoomLayoutTile m_tile;

        /// <summary>
        /// All active room tiles in the map layout.
        /// </summary>
        private Dictionary<RoomObject, RoomLayoutTile> m_roomTiles = new();

        /// <summary>
        /// The current target tile. The tile the player is in.
        /// </summary>
        private RoomLayoutTile m_targetTile;

        /// <summary>
        /// The container for all room tiles.
        /// </summary>
        [SerializeField]
        private RectTransform m_container;

        /// <summary>
        /// Generates a map layout based on a list of cells.
        /// </summary>
        /// <param name="rooms"></param>
        /// <param name="gridSize"></param>
        public void GenerateLayout(List<RoomObject> rooms, int gridSize)
        {
            ClearLayout();

            float gridHalfSize = gridSize / 2.0f;
            float tileHalfSize = m_tileSize / 2.0f;

            foreach (RoomObject room in rooms)
            {
                RoomLayoutTile tile = Instantiate(m_tile, m_container);

                tile.Initialize(MatchTileRule(room));
                ((RectTransform)tile.transform).anchoredPosition = new(tileHalfSize - m_tileSize * (gridHalfSize - room.GenerationCell.Coordinate.x), tileHalfSize - m_tileSize * (gridHalfSize - room.GenerationCell.Coordinate.y));

                room.OnPlayerEnter.AddListener(SetTargetRoom);

                m_roomTiles.Add(room, tile);
            }

            SetTargetRoom(rooms.First());
        }

        /// <summary>
        /// Sets the target map room. Repositions the map and highlights the room.
        /// </summary>
        /// <param name="room"></param>
        public void SetTargetRoom(RoomObject room)
        {
            if (m_roomTiles.TryGetValue(room, out RoomLayoutTile tile))
            {
                if (m_targetTile != null)
                    m_targetTile.Highlight(false);

                tile.Highlight(true);
                m_targetTile = tile;
            }
        }

        /// <summary>
        /// Returns the matching tile rule. This rule depends on which walls are enabled/disabled in the cell.
        /// </summary>
        /// <param name="room"></param>
        /// <returns>The matching tile rule.</returns>
        public RoomLayoutTileRule MatchTileRule(RoomObject room)
        {
            RoomLayoutTileRule match = m_rules.First();

            foreach (RoomLayoutTileRule rule in m_rules)
            {
                bool isMatch = true;

                foreach(Prims.Cell.WallDirection key in room.GenerationCell.Walls.Keys)
                    if (room.GenerationCell.Walls.GetValueOrDefault(key) != rule.RequiredWalls.GetValueOrDefault(key))
                    {
                        isMatch = false;
                        break;
                    }

                if (isMatch)
                {
                    match = rule;
                    break;
                }
            }

            return match;
        }

        /// <summary>
        /// Clears the current map layout.
        /// </summary>
        private void ClearLayout()
        {
            foreach (KeyValuePair<RoomObject, RoomLayoutTile> pair in m_roomTiles)
            {
                pair.Key.OnPlayerEnter.RemoveListener(SetTargetRoom);
                Destroy(pair.Value.gameObject);
            }

            m_roomTiles.Clear();
        }
    }
}
