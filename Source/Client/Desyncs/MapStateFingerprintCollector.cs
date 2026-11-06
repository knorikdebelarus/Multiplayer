using System.Collections.Generic;
using System.Linq;
using Multiplayer.Common.Networking.Packet;
using Verse;

namespace Multiplayer.Client.Desyncs;

/// <summary>
/// Experimental, opt-in sampler run after completed global simulation ticks.
/// Reads only spawned objects already registered with each map.
/// </summary>
internal static class MapStateFingerprintCollector
{
    internal const int SampleIntervalTicks = 600;

    internal static StateFingerprintSample Capture(int tick)
    {
        var result = new StateFingerprintSample
        {
            tick = tick,
            mapFingerprints = new List<MapStateFingerprint>()
        };

        if (Find.Maps == null)
            return result;

        foreach (var map in Find.Maps.OrderBy(m => m.uniqueID))
        {
            var digest = new MapFingerprintAccumulator(map.uniqueID);
            foreach (var thing in map.listerThings.AllThings)
            {
                if (thing == null || !thing.Spawned || thing.def == null)
                    continue;
                var pos = thing.Position;
                digest.AddThing(
                    thing.thingIDNumber,
                    thing.def.defName,
                    pos.x,
                    pos.z,
                    thing.stackCount,
                    thing.HitPoints);
            }
            result.mapFingerprints.Add(digest.Finish());
        }

        return result;
    }
}
