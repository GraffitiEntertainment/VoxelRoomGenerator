using System.Collections.Generic;
using System.Linq;

namespace GraffitiEntertainment.VoxelRoomGenerator
{
    public class MarkerDetectionRegistry
    {
        private readonly List<(string markerName, MarkerDetectionFn detection, int priority)> registry = new();

        public void Register(string markerName, MarkerDetectionFn detection, int priority = 0)
        {
            registry.Add((markerName, detection, priority));
        }

        public IEnumerable<(string markerName, MarkerDetectionFn detection)> AllDetectors =>
            registry.OrderBy(entry => entry.priority)
                .Select(entry => (entry.markerName, entry.detection));

        public bool TryGet(string markerName, out MarkerDetectionFn fn)
        {
            var entry = registry.Find(e => e.markerName == markerName);
            fn = entry.detection;
            return fn != null;
        }
    }
}
