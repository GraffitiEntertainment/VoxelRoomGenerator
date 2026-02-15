using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Linq;
using GraffitiEntertainment.VoxelShape;

namespace GraffitiEntertainment.VoxelRoomGenerator.Tests
{
    public class VoxelMarkerGeneratorTests
    {
        private IMarkerDetector detector;
        private Vector3 voxelSize;
        private List<string> markerOrder;

        [SetUp]
        public void SetUp()
        {
            detector = VoxelMarkerGenerator.Detector;
            voxelSize = new Vector3(1f, 1f, 1f); // Match your setup or adjust
            markerOrder = detector.GetAllDetectors().Select(d => d.markerName).ToList();
        }

        [Test]
        public void Test_16x1x16_Room_MidEdge_IsWall()
        {
            // Arrange
            HashSet<Vector3> voxelSet = Create16x1x16Room();
            Vector3 midEdgeVoxel = new Vector3(0, 0, 1); // Mid-edge on x=0
            Vector3 outOfBoundsNeighbor = new Vector3(-1, 0, 1); // Outside bounds

            // Act: Simulate GenerateMarkers logic (first matching detector wins)
            string assignedMarker = null;
            foreach (var markerName in markerOrder)
            {
                if (detector.DetectMarker(markerName, midEdgeVoxel, voxelSet, voxelSize))
                {
                    assignedMarker = markerName;
                    break;
                }
            }

            // Assert
            Assert.AreEqual("Wall", assignedMarker, "Mid-edge voxel should be a Wall");
            Assert.IsTrue(detector.DetectMarker("Wall", midEdgeVoxel, voxelSet, voxelSize), "Mid-edge voxel should be a Wall");
            Assert.IsFalse(detector.DetectMarker("WallArc", midEdgeVoxel, voxelSet, voxelSize), "Mid-edge voxel should not be a WallArc");
            Assert.IsFalse(detector.DetectMarker("WallCorner", midEdgeVoxel, voxelSet, voxelSize), "Mid-edge voxel should not be a WallCorner");
            Assert.IsFalse(detector.DetectMarker("Hull", outOfBoundsNeighbor, voxelSet, voxelSize), "Out-of-bounds neighbor should not be a Hull");
        }

        [Test]
        public void Test_16x1x16_Room_Corner_IsWallCorner()
        {
            // Arrange
            HashSet<Vector3> voxelSet = Create16x1x16Room();
            Vector3 cornerVoxel = new Vector3(0, 0, 0); // Corner
            Vector3 outOfBoundsNeighbor = new Vector3(-1, 0, 0);

            // Act
            string assignedMarker = null;
            foreach (var markerName in markerOrder)
            {
                if (detector.DetectMarker(markerName, cornerVoxel, voxelSet, voxelSize))
                {
                    assignedMarker = markerName;
                    break;
                }
            }

            // Assert
            Assert.AreEqual("WallCorner", assignedMarker, "Corner voxel should be a WallCorner");
            Assert.IsTrue(detector.DetectMarker("Wall", cornerVoxel, voxelSet, voxelSize), "Corner voxel should be a Wall");
            Assert.IsTrue(detector.DetectMarker("WallCorner", cornerVoxel, voxelSet, voxelSize), "Corner voxel should be a WallCorner");
            Assert.IsFalse(detector.DetectMarker("WallArc", cornerVoxel, voxelSet, voxelSize), "Corner voxel should not be a WallArc");
            Assert.IsFalse(detector.DetectMarker("Hull", outOfBoundsNeighbor, voxelSet, voxelSize), "Out-of-bounds neighbor should not be a Hull");
        }

        [Test]
        public void Test_8x1x16_Oval_Room_CurvedEdge_IsWallArc()
        {
            // Arrange
            HashSet<Vector3> voxelSet = Create8x1x16OvalRoom();
            // Example curved voxel (adjust based on actual oval shape)
            Vector3 curvedVoxel = new Vector3(7, 0, 2);

            // Act
            string assignedMarker = null;
            foreach (var markerName in markerOrder)
            {
                if (detector.DetectMarker(markerName, curvedVoxel, voxelSet, voxelSize))
                {
                    assignedMarker = markerName;
                    break;
                }
            }

            // Assert
            Assert.AreEqual("WallArc", assignedMarker, "Curved edge voxel should be a WallArc");
            Assert.IsTrue(detector.DetectMarker("Wall", curvedVoxel, voxelSet, voxelSize), "Curved edge voxel should be a Wall");
            Assert.IsTrue(detector.DetectMarker("WallArc", curvedVoxel, voxelSet, voxelSize), "Curved edge voxel should be a WallArc");
            Assert.IsFalse(detector.DetectMarker("WallCorner", curvedVoxel, voxelSet, voxelSize), "Curved edge voxel should not be a WallCorner");
        }

        [Test]
        public void Test_8x1x16_Oval_Room_StraightEdge_IsWall()
        {
            // Arrange
            HashSet<Vector3> voxelSet = Create8x1x16OvalRoom();
            // Example straighter voxel (along x-axis)
            Vector3 straightVoxel = new Vector3(7, 0, 0);

            // Act
            string assignedMarker = null;
            foreach (var markerName in markerOrder)
            {
                if (detector.DetectMarker(markerName, straightVoxel, voxelSet, voxelSize))
                {
                    assignedMarker = markerName;
                    break;
                }
            }

            // Assert
            Assert.AreEqual("Wall", assignedMarker, "Straighter edge voxel should be a Wall");
            Assert.IsTrue(detector.DetectMarker("Wall", straightVoxel, voxelSet, voxelSize), "Straighter edge voxel should be a Wall");
            Assert.IsFalse(detector.DetectMarker("WallArc", straightVoxel, voxelSet, voxelSize), "Straighter edge voxel should not be a WallArc");
            Assert.IsFalse(detector.DetectMarker("WallCorner", straightVoxel, voxelSet, voxelSize), "Straighter edge voxel should not be a WallCorner");
        }

        private HashSet<Vector3> Create16x1x16Room()
        {
            List<Vector3> voxelCells = new List<Vector3>();
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    voxelCells.Add(new Vector3(x, 0, z));
                }
            }
            for (int x = 6; x < 10; x++)
            {
                for (int z = 6; z < 10; z++)
                {
                    voxelCells.Remove(new Vector3(x, 0, z));
                }
            }
            return new HashSet<Vector3>(voxelCells);
        }

        private HashSet<Vector3> Create8x1x16OvalRoom()
        {
            List<Vector3> voxelCells = new List<Vector3>();
            Vector3 outerSize = new Vector3(16, 2, 8);
            Vector3 innerSize = new Vector3(12, 2, 4);
            Vector3 voxelSize = new Vector3(1, 1, 1);

            var outerCells = VoxelShapeGenerator.GenerateShape(
                VoxelShapeGenerator.ShapeType.Sphere,
                outerSize,
                voxelSize
            );

            var innerCells = VoxelShapeGenerator.GenerateShape(
                VoxelShapeGenerator.ShapeType.Sphere,
                innerSize,
                voxelSize
            );

            voxelCells = VoxelShapeGenerator.SubtractVoxelCells(outerCells, innerCells, Vector3.zero);
            return new HashSet<Vector3>(voxelCells);
        }
    }
}
