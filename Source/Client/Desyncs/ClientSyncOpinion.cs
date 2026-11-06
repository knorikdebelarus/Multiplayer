using System;
using Multiplayer.Client.Desyncs;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Multiplayer.Common;
using Multiplayer.Common.Networking.Packet;
using Verse;

namespace Multiplayer.Client
{

    public class ClientSyncOpinion(int startTick)
    {
        public bool isLocalClientsOpinion;

        public int startTick = startTick;
        public List<uint> commandRandomStates = new();
        public List<uint> worldRandomStates = new();
        public List<MapRandomStateData> mapStates = new();
        public List<StateFingerprintSample> stateFingerprints = new();

        // todo Unused for now
        public List<int> pawnCapacityHashes = new();
        public List<int> pawnStatHashes = new();
        public List<int> pawnNeedHashes = new();

        // Serialized only after a desync to reduce bandwidth usage in regular gameplay (only the hashes are used) and
        // help with debugging in case something goes wrong.
        public List<StackTraceLogItem> desyncStackTraces = new();
        public List<int> desyncStackTraceHashes = new();
        public bool simulating;
        public RoundModeEnum roundMode;

        public string CheckForDesync(ClientSyncOpinion other)
        {
            if (roundMode != other.roundMode)
                return $"FP round mode doesn't match: {roundMode} != {other.roundMode}";

            if (mapStates.Count != other.mapStates.Count)
                return $"Map instances don't match (counts: first={mapStates.Count}, second={other.mapStates.Count})";

            for (int i = 0; i < mapStates.Count; i++)
            {
                var firstMap = mapStates[i];
                var secondMap = other.mapStates[i];
                if (firstMap.mapId != secondMap.mapId)
                    return $"Map instances don't match at index {i}: first={firstMap.mapId}, second={secondMap.mapId}";

                if (SequenceMismatchInspector.FirstDifference(firstMap.randomStates, secondMap.randomStates) is { } mapDiff)
                    return $"Wrong random state on map {firstMap.mapId}: {mapDiff.Format("first", "second")}";
            }

            if (SequenceMismatchInspector.FirstDifference(worldRandomStates, other.worldRandomStates) is { } worldDiff)
                return $"Wrong random state for the world: {worldDiff.Format("first", "second")}";

            if (SequenceMismatchInspector.FirstDifference(commandRandomStates, other.commandRandomStates) is { } commandDiff)
                return $"Random state from commands doesn't match: {commandDiff.Format("first", "second")}";

            var mismatch = StateFingerprintComparison.FirstMismatch(stateFingerprints, other.stateFingerprints);
            if (mismatch != null)
                return mismatch;

            return null;
        }

        public List<uint> GetRandomStatesForMap(int mapId)
        {
            var result = mapStates.Find(m => m.mapId == mapId);
            if (result != null) return result.randomStates;
            mapStates.Add(result = new MapRandomStateData(mapId));
            return result.randomStates;
        }

        public byte[] Serialize()
        {
            var writer = new ByteWriter();

            writer.WriteInt32(startTick);
            writer.WritePrefixedUInts(commandRandomStates);
            writer.WritePrefixedUInts(worldRandomStates);

            writer.WriteInt32(mapStates.Count);
            foreach (var map in mapStates)
            {
                writer.WriteInt32(map.mapId);
                writer.WritePrefixedUInts(map.randomStates);
            }

            writer.WritePrefixedInts(desyncStackTraceHashes);
            writer.WriteBool(simulating);
            writer.WriteShort((short)roundMode);
            writer.WriteInt32(stateFingerprints.Count);
            foreach (var snapshot in stateFingerprints)
            {
                writer.WriteInt32(snapshot.tick);
                writer.WriteInt32(snapshot.mapFingerprints.Count);
                foreach (var map in snapshot.mapFingerprints)
                {
                    writer.WriteInt32(map.mapId);
                    writer.WriteInt32(map.thingCount);
                    writer.WriteInt32(map.hashLow);
                    writer.WriteInt32(map.hashHigh);
                }
            }
            return writer.ToArray();
        }

        public static ClientSyncOpinion FromNet(SyncOpinion sync) => new(sync.startTick)
        {
            commandRandomStates = sync.commandRandomStates,
            worldRandomStates = sync.worldRandomStates,
            mapStates = sync.mapRandomStates.Select(state => new MapRandomStateData(state.mapId)
                { randomStates = state.randomStates }).ToList(),
            desyncStackTraceHashes = sync.traceHashes,
            simulating = sync.simulating,
            roundMode = sync.roundMode,
            stateFingerprints = sync.stateFingerprints ?? new()
        };

        public SyncOpinion ToNet() => new()
        {
            startTick = startTick,
            commandRandomStates = commandRandomStates,
            worldRandomStates = worldRandomStates,
            mapRandomStates = mapStates.Select(state => new MapRandomState
                { mapId = state.mapId, randomStates = state.randomStates }).ToList(),
            traceHashes = desyncStackTraceHashes,
            simulating = simulating,
            roundMode = roundMode,
            stateFingerprints = stateFingerprints
        };

        public static ClientSyncOpinion Deserialize(ByteReader data)
        {
            var startTick = data.ReadInt32();

            var cmds = new List<uint>(data.ReadPrefixedUInts());
            var world = new List<uint>(data.ReadPrefixedUInts());

            var maps = new List<MapRandomStateData>();
            int mapCount = data.ReadInt32();
            for (int i = 0; i < mapCount; i++)
            {
                int mapId = data.ReadInt32();
                var mapData = new List<uint>(data.ReadPrefixedUInts());
                maps.Add(new MapRandomStateData(mapId) { randomStates = mapData });
            }

            var traceHashes = new List<int>(data.ReadPrefixedInts());
            var simulating = data.ReadBool();
            var roundMode = data.ReadShort();

            // Old replays have no fingerprint data. Live networking is
            // versioned with MpVersion.Protocol independently.
            var snapshots = new List<StateFingerprintSample>();
            if (data.Left > 0)
            {
                int sampleCount = data.ReadInt32();
                if (sampleCount < 0 || sampleCount > 2048)
                    throw new ReaderException($"Invalid fingerprint sample count: {sampleCount}");

                for (int i = 0; i < sampleCount; i++)
                {
                    int tick = data.ReadInt32();
                    int snapshotMapCount = data.ReadInt32();
                    if (snapshotMapCount < 0 || snapshotMapCount > 512)
                        throw new ReaderException($"Invalid fingerprint map count: {snapshotMapCount}");

                    var snapshotMaps = new List<MapStateFingerprint>(snapshotMapCount);
                    for (int m = 0; m < snapshotMapCount; m++)
                        snapshotMaps.Add(new MapStateFingerprint
                        {
                            mapId = data.ReadInt32(),
                            thingCount = data.ReadInt32(),
                            hashLow = data.ReadInt32(),
                            hashHigh = data.ReadInt32()
                        });
                    snapshots.Add(new StateFingerprintSample { tick = tick, mapFingerprints = snapshotMaps });
                }
            }

            return new ClientSyncOpinion(startTick)
            {
                commandRandomStates = cmds,
                worldRandomStates = world,
                mapStates = maps,
                desyncStackTraceHashes = traceHashes,
                simulating = simulating,
                roundMode = (RoundModeEnum)roundMode,
                stateFingerprints = snapshots
            };
        }

        public void TryMarkSimulating()
        {
            if (TickPatch.Simulating)
                simulating = true;
        }

        public string GetFormattedStackTracesForRange(int diffAt)
        {
            var start = Math.Max(0, diffAt - Multiplayer.settings.desyncTracesRadius);
            var end = diffAt + Multiplayer.settings.desyncTracesRadius;
            var traceId = start;

            return
                $"Trace count: {desyncStackTraces.Count}\nTrace of first desynced map random state:\n{diffAt} {desyncStackTraces.ElementAtOrDefault(diffAt)}" +
                "\n\nContext traces:\n" +
                desyncStackTraces
                .Skip(start)
                .Take(end - start)
                .Join(a => traceId++ + " " + a, "\n\n");
        }

        public void Clear()
        {
            for (int i = 0; i < desyncStackTraces.Count; i++)
                desyncStackTraces[i].Dispose();
        }
    }
}
