using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GraffitiEntertainment.VoxelRoomGenerator
{
    public static class VoxelMarkerGenerator
    {
        private static readonly MarkerDetectionRegistry registry = new MarkerDetectionRegistry();
        private static readonly IMarkerDetector detector;

        static VoxelMarkerGenerator()
        {
            detector = new MarkerDetector(registry);
        }

        public static List<Marker> GenerateMarkers(List<Vector3> voxelCells, Vector3 voxelSize)
        {
            var voxelSet = new HashSet<Vector3>(voxelCells);
            var markers = new List<Marker>();

            var scanBounds = CalculateBounds(voxelCells);

            for (float x = scanBounds.min.x; x <= scanBounds.max.x; x += voxelSize.x)
            {
                for (float y = scanBounds.min.y; y <= scanBounds.max.y; y += voxelSize.y)
                {
                    for (float z = scanBounds.min.z; z <= scanBounds.max.z; z += voxelSize.z)
                    {
                        var cell = new Vector3(x, y, z);

                        foreach (var (markerName, _) in detector.GetAllDetectors())
                        {
                            if (detector.DetectMarker(markerName, cell, voxelSet, voxelSize))
                            {
                                Quaternion rotation = DetermineWallRotation(cell, voxelSize, voxelSet);
                                markers.Add(new Marker(cell, rotation, markerName));
                                break;
                            }
                        }
                    }
                }
            }

            return markers;
        }

        public static MarkerDetectionRegistry Registry => registry;

        public static IMarkerDetector Detector => detector;

        private static readonly Vector3[] directions = new[]
        {
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        private static Quaternion DetermineWallRotation(Vector3 cell, Vector3 voxelSize, HashSet<Vector3> voxelSet)
        {
            foreach (var dir in directions)
            {
                Vector3 neighbor = cell + Vector3.Scale(dir, voxelSize);
                if (!voxelSet.Contains(neighbor))
                {
                    return Quaternion.LookRotation(-dir);
                }
            }
            return Quaternion.identity;
        }

        private static Bounds CalculateBounds(List<Vector3> cells)
        {
            if (cells.Count == 0)
            {
                return new Bounds();
            }

            var bounds = new Bounds(cells[0], Vector3.zero);
            foreach (var cell in cells)
            {
                bounds.Encapsulate(cell);
            }
            return bounds;
        }
    }
}
