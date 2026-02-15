using System.Collections.Generic;
using UnityEngine;

namespace GraffitiEntertainment.VoxelRoomGenerator
{
    public delegate bool MarkerDetectionFn(UnityEngine.Vector3 cell, HashSet<Vector3> voxelSet, Vector3 voxelSize);
}