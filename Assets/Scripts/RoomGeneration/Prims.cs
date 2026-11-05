using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.RoomGeneration
{
    /// <summary>
    /// Random implementation of Prim's Algorithm.
    /// </summary>
    // Reference Source: https://weblog.jamisbuck.org/2011/1/10/maze-generation-prim-s-algorithm
    public struct Prims
    {
        /// <summary>
        /// All directions to each neighbor of a cell
        /// </summary>
        private static readonly Vector2Int[] NeighborDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public sealed class Cell
        {
            /// <summary>
            /// Converts a Vector2Int direction to a matching wall direction.
            /// </summary>
            private static readonly Dictionary<Vector2Int, Direction> VectorToDirection = new()
            {
                [Vector2Int.up] = Direction.North,
                [Vector2Int.down] = Direction.South,
                [Vector2Int.right] = Direction.East,
                [Vector2Int.left] = Direction.West,
            };

            /// <summary>
            /// The opposite directions of each wall direction.
            /// </summary>
            private static readonly Dictionary<Direction, Direction> OppositeDirections = new()
            {
                [Direction.North] = Direction.South,
                [Direction.South] = Direction.North,
                [Direction.East] = Direction.West,
                [Direction.West] = Direction.East
            };

            /// <summary>
            /// The directions of each wall. There is only four walls per cell.
            /// </summary>
            public enum Direction
            {
                North,
                South,
                East,
                West
            }

            /// <summary>
            /// The walls of the cell.
            /// </summary>
            public Dictionary<Direction, bool> Walls { get; private set; }
            
            /// <summary>
            /// The coordinate of the cell.
            /// </summary>
            public Vector2Int Coordinate { get; private set; }

            /// <summary>
            /// Whether the cell has been visited.
            /// </summary>
            public bool Visited;

            /// <summary>
            /// Constructs a cell with a specified x and y coordinate.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public Cell(int x, int y) 
            {
                Walls = new();
                Coordinate = new(x, y);

                foreach (Direction wall in Enum.GetValues(typeof(Direction)))
                    Walls.Add(wall, true);
            }

            /// <summary>
            /// Flips a direction.
            /// </summary>
            /// <param name="direction"></param>
            /// <returns>The flipped direction.</returns>
            public static Direction FlipDirection(Direction direction)
            {
                return OppositeDirections[direction];
            }

            /// <summary>
            /// Returns the direction from a to b.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns>The direction from a to b.</returns>
            public static Direction GetDirectionBetweenCells(Cell a, Cell b)
            {
                Vector2Int normal = (b.Coordinate - a.Coordinate).Normalized();
                return VectorToDirection[normal];
            }
        }

        /// <summary>
        /// The grid Prim's Algorithm is used on.
        /// </summary>
        private Cell[,] m_grid;

        /// <summary>
        /// The size of the grid.
        /// </summary>
        private int m_size;

        private System.Random m_generator;

        /// <summary>
        /// Constructs of a specified size grid that Prim's algorithm will be used on.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="seed"></param>
        public Prims(int size, int seed = 0)
        {
            m_generator = new(seed);
            m_grid = new Cell[size, size];
            m_size = size;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    m_grid[x, y] = new Cell(x, y);
        }

        /// <summary>
        /// Generates a random maze using Prim's Algorithm.
        /// </summary>
        /// <param name="maxDepth"></param>
        /// <returns>All of the cells that were modified in the grid.</returns>
        public readonly List<Cell> Generate(int maxDepth) 
        {
            List<Cell> visited = new(), frontier = new();

            MarkCell(m_grid[m_generator.Next(0, m_size), m_generator.Next(0, m_size)], frontier, visited);

            while (!frontier.IsEmpty()) 
            {
                Cell current = frontier.PopRandomElement(m_generator);

                List<Cell> neighbors = GetNeighborsOfCell(current);

                if (!neighbors.IsEmpty())
                {
                    Cell neighbor = neighbors.GetRandomElement(m_generator);

                    Cell.Direction direction = Cell.GetDirectionBetweenCells(current, neighbor);

                    current.Walls[direction] = false;
                    neighbor.Walls[Cell.FlipDirection(direction)] = false;
                }

                MarkCell(current, frontier, visited);
            }

            visited.Reverse();

            return visited;
        }

        /// <summary>
        /// Marks a cell as visited and adds it's neighbors to the frontier.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="frontier"></param>
        /// <param name="visited"></param>
        private readonly void MarkCell(Cell cell, List<Cell> frontier, List<Cell> visited)
        {
            cell.Visited = true;

            foreach (Cell neighbor in GetNeighborsOfCell(cell))
                frontier.Add(neighbor);

            visited.Add(cell);
        }

        /// <summary>
        /// Returns whether a coordinate is valid within the bounds of the grid.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns>Whether a coordinate is valid.</returns>
        private readonly bool IsValidCordinate(Vector2Int coordinate) 
        {
            return coordinate.x >= 0 && coordinate.y >= 0 && coordinate.x < m_size && coordinate.y < m_size;
        }

        /// <summary>
        /// Returns all of the not visited neighbors of a cell.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns>The neighbors of a cell.</returns>
        private readonly List<Cell> GetNeighborsOfCell(Cell cell)
        {
            List<Cell> neighbors = new();

            foreach (Vector2Int direction in NeighborDirections) 
            { 
                Vector2Int neighborCord = cell.Coordinate + direction;

                if (IsValidCordinate(neighborCord) && !m_grid[neighborCord.x, neighborCord.y].Visited)
                    neighbors.Add(m_grid[neighborCord.x, neighborCord.y]);
            }

            return neighbors;
        }
    }
}
