using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Random implementation of Prim's Algorithm.
    /// </summary>
    // Implementation Source: https://weblog.jamisbuck.org/2011/1/10/maze-generation-prim-s-algorithm
    public sealed class Prims
    {
        /// <summary>
        /// All directions to each neighbor of a cell
        /// </summary>
        private static readonly Vector2Int[] NeighborDirections = {
            Vector2Int.down,
            Vector2Int.up, 
            Vector2Int.left, 
            Vector2Int.right 
        };

        [Serializable]
        public sealed class Cell
        {
            /// <summary>
            /// The available states of a cell.
            /// </summary>
            public enum CellState
            {
                Unvisited,
                Visited,
                InFrontier
            }

            /// <summary>
            /// The directions of each wall. There is only four walls per cell.
            /// </summary>
            public enum WallDirection
            {
                North,
                South,
                East,
                West
            }

            /// <summary>
            /// Converts a Vector2Int direction to a matching wall direction.
            /// </summary>
            private static readonly Dictionary<Vector2Int, WallDirection> VectorToDirection = new()
            {
                [Vector2Int.up] = WallDirection.North,
                [Vector2Int.down] = WallDirection.South,
                [Vector2Int.right] = WallDirection.East,
                [Vector2Int.left] = WallDirection.West,
            };

            /// <summary>
            /// The opposite directions of each wall direction.
            /// </summary>
            private static readonly Dictionary<WallDirection, WallDirection> OppositeDirections = new()
            {
                [WallDirection.North] = WallDirection.South,
                [WallDirection.South] = WallDirection.North,
                [WallDirection.East] = WallDirection.West,
                [WallDirection.West] = WallDirection.East
            };

            /// <summary>
            /// The walls of the cell.
            /// </summary>
            public Dictionary<WallDirection, bool> Walls { get; private set; }
            
            /// <summary>
            /// The coordinate of the cell.
            /// </summary>
            public Vector2Int Coordinate { get; private set; }

            /// <summary>
            /// The current state of the cell.
            /// </summary>
            public CellState State;

            /// <summary>
            /// Constructs a cell with a specified x and y coordinate.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public Cell(int x, int y) 
            {
                Walls = new();
                Coordinate = new(x, y);

                foreach (WallDirection wall in Enum.GetValues(typeof(WallDirection)))
                    Walls.Add(wall, true);
            }

            /// <summary>
            /// Flips a direction.
            /// </summary>
            /// <param name="direction"></param>
            /// <returns>The flipped direction.</returns>
            public static WallDirection FlipDirection(WallDirection direction)
            {
                return OppositeDirections[direction];
            }

            /// <summary>
            /// Returns the direction from a to b.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns>The direction from a to b.</returns>
            public static WallDirection GetDirectionBetweenCells(Cell a, Cell b)
            {
                Vector2Int normal = (b.Coordinate - a.Coordinate).Normalized();
                return VectorToDirection[normal];
            }
        }

        /// <summary>
        /// The grid Prim's Algorithm is used on.
        /// </summary>
        public readonly Cell[,] Grid;

        /// <summary>
        /// The size of the grid.
        /// </summary>
        private readonly int m_size;

        /// <summary>
        /// Constructs a new grid that Prim's Algorithm will modify.
        /// </summary>
        /// <param name="size"></param>
        public Prims(int size)
        {
            Grid = new Cell[size, size];
            m_size = size;
        }

        /// <summary>
        /// Generates a random maze using Prim's Algorithm. Uses a custom Random generator.
        /// </summary>
        /// <param name="maxCells"></param>
        /// <param name="generator"></param>
        /// <returns>The path generated in the maze.</returns>
        public List<Cell> Generate(int maxCells, System.Random generator) 
        {
            List<Cell> path = new();
            List<Cell> frontier = new();

            void VisitCell(Cell cell)
            {
                cell.State = Cell.CellState.Visited;

                foreach (Cell neighbor in GetNeighborsOfCell(cell, Cell.CellState.Unvisited))
                {
                    neighbor.State = Cell.CellState.InFrontier;
                    frontier.Add(neighbor);
                }

                path.Add(cell);
            }

            ResetGrid();
            VisitCell(Grid[m_size/2, m_size/2]);

            while (!frontier.IsEmpty() && path.Count < maxCells) 
            {
                Cell current = frontier.PopRandomElement(generator);

                List<Cell> neighbors = GetNeighborsOfCell(current, Cell.CellState.Visited);

                if (!neighbors.IsEmpty())
                {
                    Cell neighbor = neighbors.GetRandomElement(generator);

                    Cell.WallDirection direction = Cell.GetDirectionBetweenCells(current, neighbor);

                    current.Walls[direction] = false;
                    neighbor.Walls[Cell.FlipDirection(direction)] = false;
                }

                VisitCell(current);
            }

            return path;
        }

        /// <summary>
        /// Resets the grid.
        /// </summary>
        private void ResetGrid()
        {
            for (int x = 0; x < m_size; x++)
                for (int y = 0; y < m_size; y++)
                    Grid[x, y] = new Cell(x, y);
        }

        /// <summary>
        /// Returns whether a coordinate is valid within the bounds of the grid.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns>Whether a coordinate is valid.</returns>
        private bool IsValidCordinate(Vector2Int coordinate) 
        {
            return coordinate.x >= 0 && coordinate.y >= 0 && coordinate.x < m_size && coordinate.y < m_size;
        }

        /// <summary>
        /// Returns all of the not visited neighbors of a cell. Optionally, you can target cells with a certain state.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="targetState"></param>
        /// <returns>The neighbors of a cell.</returns>
        private List<Cell> GetNeighborsOfCell(Cell cell, Cell.CellState targetState = Cell.CellState.Unvisited)
        {
            List<Cell> neighbors = new();

            foreach (Vector2Int direction in NeighborDirections) 
            { 
                Vector2Int neighborCord = cell.Coordinate + direction;

                if (IsValidCordinate(neighborCord) && Grid[neighborCord.x, neighborCord.y].State == targetState)
                    neighbors.Add(Grid[neighborCord.x, neighborCord.y]);
            }

            return neighbors;
        }
    }
}
