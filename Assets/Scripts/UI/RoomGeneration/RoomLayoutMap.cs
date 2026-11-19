using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        /// Generates a map layout based on a list of cells.
        /// </summary>
        /// <param name="layout"></param>
        public void GenerateLayout(List<Prims.Cell> layout, int gridSize)
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            foreach (Prims.Cell cell in layout)
            {
                RoomLayoutTile tile = Instantiate(m_tile, transform);

                tile.Initialize(MatchTileRule(cell));

                ((RectTransform)tile.transform).anchoredPosition = new(m_tileSize/2 - m_tileSize * (gridSize/2 - cell.Coordinate.x), m_tileSize/2 - m_tileSize * (gridSize / 2 - cell.Coordinate.y));
            }
        }

        public RoomLayoutTileRule MatchTileRule(Prims.Cell cell)
        {
            RoomLayoutTileRule match = m_rules.First();

            foreach (RoomLayoutTileRule rule in m_rules)
            {
                bool isMatch = true;

                foreach(Prims.Cell.WallDirection key in cell.Walls.Keys)
                    if (cell.Walls.GetValueOrDefault(key) != rule.RequiredWalls.GetValueOrDefault(key))
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
    }
}
