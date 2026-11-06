using Multiplayer.Common.Networking.Packet;

namespace Multiplayer.Client.Desyncs;

/// <summary>
/// Stable 64-bit FNV-1a over explicitly encoded primitives.
/// Never use CLR object hashes, culture-sensitive formatting, or RNG here.
/// </summary>
internal struct StableFingerprintHasher
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;
    private ulong value;

    public StableFingerprintHasher() => value = Offset;
    internal ulong Value => value;

    internal void AddInt(int input)
    {
        unchecked
        {
            uint bits = (uint)input;
            for (int i = 0; i < 4; i++)
            {
                value ^= (byte)(bits >> (i * 8));
                value *= Prime;
            }
        }
    }

    internal void AddULong(ulong input)
    {
        unchecked
        {
            for (int i = 0; i < 8; i++)
            {
                value ^= (byte)(input >> (i * 8));
                value *= Prime;
            }
        }
    }

    internal void AddString(string? input)
    {
        if (input == null)
        {
            AddInt(-1);
            return;
        }

        AddInt(input.Length);
        foreach (char c in input)
            AddInt(c);
    }
}

/// <summary>
/// Order-independent aggregate: no sorting or per-Thing list allocation.
/// Includes two commutative aggregates and a count before final mixing.
/// </summary>
internal struct MapFingerprintAccumulator
{
    private readonly int mapId;
    private ulong sum;
    private ulong rotatedXor;
    private int count;

    internal MapFingerprintAccumulator(int mapId)
    {
        this.mapId = mapId;
        sum = 0;
        rotatedXor = 0;
        count = 0;
    }

    internal void AddThing(int id, string? defName, int x, int z, int stackCount, int hitPoints)
    {
        var item = new StableFingerprintHasher();
        item.AddInt(id);
        item.AddString(defName);
        item.AddInt(x);
        item.AddInt(z);
        item.AddInt(stackCount);
        item.AddInt(hitPoints);
        ulong digest = item.Value;
        unchecked { sum += digest; }
        rotatedXor ^= RotateLeft(digest, id & 63);
        count++;
    }

    internal MapStateFingerprint Finish()
    {
        var digest = new StableFingerprintHasher();
        digest.AddInt(mapId);
        digest.AddInt(count);
        digest.AddULong(sum);
        digest.AddULong(rotatedXor);
        ulong hash = digest.Value;
        return new MapStateFingerprint
        {
            mapId = mapId,
            thingCount = count,
            hashLow = unchecked((int)hash),
            hashHigh = unchecked((int)(hash >> 32))
        };
    }

    private static ulong RotateLeft(ulong bits, int shift) =>
        shift == 0 ? bits : (bits << shift) | (bits >> (64 - shift));
}
