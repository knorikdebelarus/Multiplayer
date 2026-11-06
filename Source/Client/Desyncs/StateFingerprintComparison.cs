using System.Collections.Generic;
using Multiplayer.Common.Networking.Packet;

namespace Multiplayer.Client.Desyncs;

/// <summary>
/// Compare only samples with the same global multiplayer tick.
/// Unmatched samples are ignored to avoid false desyncs during catch-up/rejoin
/// or when one client has the experimental feature disabled.
/// </summary>
internal static class StateFingerprintComparison
{
    internal static string? FirstMismatch(
        IReadOnlyList<StateFingerprintSample> local,
        IReadOnlyList<StateFingerprintSample> remote)
    {
        foreach (var a in local)
        {
            StateFingerprintSample b = default;
            bool matched = false;
            foreach (var candidate in remote)
            {
                if (candidate.tick != a.tick) continue;
                b = candidate;
                matched = true;
                break;
            }

            if (!matched) continue;
            foreach (var mapA in a.mapFingerprints)
            {
                bool found = false;
                foreach (var mapB in b.mapFingerprints)
                {
                    if (mapA.mapId != mapB.mapId) continue;
                    found = true;
                    if (mapA.thingCount != mapB.thingCount ||
                        mapA.hashLow != mapB.hashLow ||
                        mapA.hashHigh != mapB.hashHigh)
                        return $"Map state differs at tick {a.tick} on map {mapA.mapId}: " +
                            $"objects local={mapA.thingCount}, remote={mapB.thingCount}; " +
                            $"hash local={mapA.hashHigh:X8}{mapA.hashLow:X8}, " +
                            $"remote={mapB.hashHigh:X8}{mapB.hashLow:X8}";
                    break;
                }

                if (!found)
                    return $"Map state differs at tick {a.tick}: map {mapA.mapId} missing remotely";
            }
            foreach (var mapB in b.mapFingerprints)
            {
                bool found = false;
                foreach (var mapA in a.mapFingerprints)
                    if (mapA.mapId == mapB.mapId) { found = true; break; }

                if (!found)
                    return $"Map state differs at tick {a.tick}: map {mapB.mapId} missing locally";
            }
        }
        return null;
    }
}
