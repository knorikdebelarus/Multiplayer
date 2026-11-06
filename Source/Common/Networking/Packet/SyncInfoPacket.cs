using System.Collections.Generic;

namespace Multiplayer.Common.Networking.Packet;

[PacketDefinition(Packets.Client_SyncInfo, allowFragmented: true)]
public record struct ClientSyncInfoPacket : IPacket
{
    // 8MiB. For some big saves, the sync opinion can reach ~2.5MiB, so this includes some leeway.
    public const int MaxLength = 1 << 23;
    public byte[] rawSyncOpinion;

    public SyncOpinion SyncOpinion
    {
        get => SyncOpinionBinder.Deserialize(rawSyncOpinion);
        set => rawSyncOpinion = SyncOpinionBinder.Serialize(value);
    }

    public void Bind(PacketBuffer buf)
    {
        buf.BindRemaining(ref rawSyncOpinion, maxLength: MaxLength);
    }

    private static readonly Binder<SyncOpinion> SyncOpinionBinder = BinderOf.Identity<SyncOpinion>();
}

[PacketDefinition(Packets.Server_SyncInfo, allowFragmented: true)]
public record struct ServerSyncInfoPacket : IPacket
{
    public byte[] rawSyncOpinion;
    public SyncOpinion SyncOpinion
    {
        get => SyncOpinionBinder.Deserialize(rawSyncOpinion);
        set => rawSyncOpinion = SyncOpinionBinder.Serialize(value);
    }

    public void Bind(PacketBuffer buf)
    {
        buf.BindRemaining(ref rawSyncOpinion, maxLength: ClientSyncInfoPacket.MaxLength);
    }

    private static readonly Binder<SyncOpinion> SyncOpinionBinder = BinderOf.Identity<SyncOpinion>();
}

public record struct SyncOpinion : IPacketBufferable
{
    public int startTick;
    public List<uint> commandRandomStates;
    public List<uint> worldRandomStates;
    public List<MapRandomState> mapRandomStates;
    public List<int> traceHashes;
    public bool simulating;
    public RoundModeEnum roundMode;
    public List<StateFingerprintSample> stateFingerprints;

    public void Bind(PacketBuffer buf)
    {
        buf.Bind(ref startTick);

        buf.Bind(ref commandRandomStates, BinderOf.UInt());
        buf.Bind(ref worldRandomStates, BinderOf.UInt());
        buf.Bind(ref mapRandomStates, BinderOf.Identity<MapRandomState>());
        // Max 2M trace hashes. For some big saves, the trace hashes can reach ~600k entries, so this includes
        // some leeway.
        buf.Bind(ref traceHashes, BinderOf.Int(), maxLength: 1<<21);

        buf.Bind(ref simulating);
        buf.BindEnum(ref roundMode);
        buf.Bind(ref stateFingerprints, BinderOf.Identity<StateFingerprintSample>(), maxLength: 2048);
    }
}

/// <summary>Snapshot of spawned map objects at a global multiplayer tick.</summary>
public record struct StateFingerprintSample : IPacketBufferable
{
    public int tick;
    public List<MapStateFingerprint> mapFingerprints;

    public void Bind(PacketBuffer buf)
    {
        buf.Bind(ref tick);
        buf.Bind(ref mapFingerprints, BinderOf.Identity<MapStateFingerprint>(), maxLength: 512);
    }
}

public record struct MapStateFingerprint : IPacketBufferable
{
    public int mapId;
    public int thingCount;
    public int hashLow;
    public int hashHigh;

    public void Bind(PacketBuffer buf)
    {
        buf.Bind(ref mapId);
        buf.Bind(ref thingCount);
        buf.Bind(ref hashLow);
        buf.Bind(ref hashHigh);
    }
}

public record struct MapRandomState : IPacketBufferable
{
    public int mapId;
    public List<uint> randomStates;

    public void Bind(PacketBuffer buf)
    {
        buf.Bind(ref mapId);
        buf.Bind(ref randomStates, BinderOf.UInt());
    }
}
