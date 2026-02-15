using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GraffitiEntertainment.VoxelRoomGenerator
{
    public interface IMarkerDetector
    {
        bool DetectMarker(string markerName, Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize);
        IEnumerable<(string markerName, MarkerDetectionFn)> GetAllDetectors();
    }

    public class MarkerDetector : IMarkerDetector
    {
        private readonly MarkerDetectionRegistry registry;
        private static readonly Vector3[] directions = new[]
        {
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        public MarkerDetector(MarkerDetectionRegistry registry = null)
        {
            this.registry = registry ?? new MarkerDetectionRegistry();
            if (!this.registry.AllDetectors.Any())
            {
                RegisterDefaultDetectors();
            }
        }

        public bool DetectMarker(string markerName, Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            if (registry.TryGet(markerName, out MarkerDetectionFn fn))
            {
                return fn(cell, voxelSet, voxelSize);
            }
            return false;
        }

        public IEnumerable<(string markerName, MarkerDetectionFn)> GetAllDetectors()
        {
            return registry.AllDetectors;
        }

        private void RegisterDefaultDetectors()
        {
            // Priorities: Lower runs first
            registry.Register("Hull", IsHull, 0); // Highest priority
            registry.Register("Floor", IsFloor, 0);
            registry.Register("Wall", IsWall, 1); // Walls after Hull/Floor
            registry.Register("WallArc", IsWallArc, 2); // Specialized walls last
            registry.Register("WallCorner", IsWallCorner, 2);
            registry.Register("Wall45", IsWall45, 2);
        }

        private bool IsHull(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            if (voxelSet.Contains(cell))
            {
                return false;
            }

            foreach (var dir in directions)
            {
                Vector3 neighbor = cell + Vector3.Scale(dir, voxelSize);
                if (voxelSet.Contains(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFloor(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            if (!voxelSet.Contains(cell))
            {
                return false;
            }

            return AllNeighborsPresent(cell, voxelSet, voxelSize);
        }

        private bool IsWall(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            if (!voxelSet.Contains(cell))
            {
                return false;
            }

            // Wall if adjacent to a Hull voxel or an out-of-bounds empty space
            Bounds bounds = CalculateBounds(voxelSet);
            foreach (var dir in directions)
            {
                Vector3 neighbor = cell + Vector3.Scale(dir, voxelSize);
                if (IsHull(neighbor, voxelSet, voxelSize) || !bounds.Contains(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsWallArc(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            if (!IsWall(cell, voxelSet, voxelSize))
            {
                return false;
            }

            int neighborCount = 0;
            bool[] hasNeighbor = new bool[4];
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3 neighbor = cell + Vector3.Scale(directions[i], voxelSize);
                if (voxelSet.Contains(neighbor))
                {
                    neighborCount++;
                    hasNeighbor[i] = true;
                }
            }

            if (neighborCount != 3)
            {
                return false;
            }

            int exposedDirIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                if (!hasNeighbor[i])
                {
                    exposedDirIndex = i;
                    break;
                }
            }

            return CheckCurve(cell, voxelSet, voxelSize, exposedDirIndex);
        }

        private bool IsWallCorner(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            if (!IsWall(cell, voxelSet, voxelSize))
            {
                return false;
            }

            int exposedSides = 0;
            Bounds bounds = CalculateBounds(voxelSet);
            foreach (var dir in directions)
            {
                Vector3 neighbor = cell + Vector3.Scale(dir, voxelSize);
                if (!voxelSet.Contains(neighbor))
                {
                    exposedSides++;
                }
            }

            return exposedSides >= 2 && !IsWall45(cell, voxelSet, voxelSize);
        }

        private bool IsWall45(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            if (!IsWall(cell, voxelSet, voxelSize))
            {
                return false;
            }

            var cornerDirs = new[]
            {
                Vector3.left + Vector3.forward,
                Vector3.left + Vector3.back,
                Vector3.right + Vector3.forward,
                Vector3.right + Vector3.back
            };

            foreach (var corner in cornerDirs)
            {
                var diag = cell + Vector3.Scale(corner, voxelSize);
                var adj1 = cell + Vector3.Scale(new Vector3(corner.x, 0, 0), voxelSize);
                var adj2 = cell + Vector3.Scale(new Vector3(0, 0, corner.z), voxelSize);

                if (!voxelSet.Contains(diag) && voxelSet.Contains(adj1) && voxelSet.Contains(adj2))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckCurve(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize, int exposedDirIndex)
        {
            int forward = (exposedDirIndex + 1) % 4;
            int back = (exposedDirIndex + 3) % 4;
            Vector3[] traceDirections = { directions[forward], directions[back] };

            List<Vector3> edgePath = new List<Vector3> { cell };
            foreach (var traceDir in traceDirections)
            {
                Vector3 current = cell;
                for (int step = 0; step < 3; step++)
                {
                    Vector3 next = current + Vector3.Scale(traceDir, voxelSize);
                    if (voxelSet.Contains(next) && HasExposedSide(next, voxelSet, voxelSize))
                    {
                        edgePath.Add(next);
                        current = next;
                    }
                    else
                    {
                        int perp1 = (Array.IndexOf(directions, traceDir) + 1) % 4;
                        int perp2 = (Array.IndexOf(directions, traceDir) + 3) % 4;
                        Vector3[] perps = { directions[perp1], directions[perp2] };
                        bool found = false;
                        foreach (var perp in perps)
                        {
                            next = current + Vector3.Scale(perp, voxelSize);
                            if (voxelSet.Contains(next) && HasExposedSide(next, voxelSet, voxelSize))
                            {
                                edgePath.Add(next);
                                current = next;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            break;
                        }
                    }
                }
            }

            if (edgePath.Count < 3)
            {
                return false;
            }

            for (int i = 1; i < edgePath.Count - 1; i++)
            {
                Vector3 prev = edgePath[i - 1] - edgePath[i];
                Vector3 next = edgePath[i + 1] - edgePath[i];
                float angle = Vector3.Angle(prev, next);
                if (angle > 30f && angle < 150f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasExposedSide(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            Bounds bounds = CalculateBounds(voxelSet);
            foreach (var dir in directions)
            {
                Vector3 neighbor = cell + Vector3.Scale(dir, voxelSize);
                if (!voxelSet.Contains(neighbor) && bounds.Contains(neighbor))
                {
                    return true;
                }
            }
            return false;
        }

        private bool AllNeighborsPresent(Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize)
        {
            Bounds bounds = CalculateBounds(voxelSet);
            foreach (var dir in directions)
            {
                Vector3 neighbor = cell + Vector3.Scale(dir, voxelSize);
                if (!voxelSet.Contains(neighbor) && bounds.Contains(neighbor))
                {
                    return false;
                }
            }
            return true;
        }

        private Bounds CalculateBounds(HashSet<Vector3> voxelSet)
        {
            if (voxelSet.Count == 0)
            {
                return new Bounds();
            }

            var bounds = new Bounds(voxelSet.First(), Vector3.zero);
            foreach (var cell in voxelSet)
            {
                bounds.Encapsulate(cell);
            }
            return bounds;
        }
    }
}
