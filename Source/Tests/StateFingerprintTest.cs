using Multiplayer.Client.Desyncs;
using Multiplayer.Common.Networking.Packet;

namespace Tests;

public class StateFingerprintTest
{
    private static MapStateFingerprint Make(params (int id, string def, int x, int z, int stack, int hp)[] things)
    {
        var hash = new MapFingerprintAccumulator(123);
        foreach (var t in things)
            hash.AddThing(t.id, t.def, t.x, t.z, t.stack, t.hp);
        return hash.Finish();
    }

    [Test]
    public void SameThingsInDifferentIterationOrderHashIdentically()
    {
        var first = Make((5, "Steel", 2, 3, 5, 100), (3, "Pawn", 1, 1, 1, 99));
        var second = Make((3, "Pawn", 1, 1, 1, 99), (5, "Steel", 2, 3, 5, 100));
        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.thingCount, Is.EqualTo(2));
    }

    [Test]
    public void ChangesToPositionStackDefinitionAndHealthChangeHash()
    {
        var first = Make((5, "Steel", 2, 3, 5, 100));
        var variations = new[]
        {
            Make((5, "Steel", 3, 3, 5, 100)),
            Make((5, "Steel", 2, 3, 6, 100)),
            Make((5, "Component", 2, 3, 5, 100)),
            Make((5, "Steel", 2, 3, 5, 99)),
            Make((5, "Steel", 2, 3, 5, 100), (6, "Stone", 2, 4, 1, 100))
        };
        foreach (var variation in variations)
            Assert.That((variation.thingCount, variation.hashHigh, variation.hashLow),
                Is.Not.EqualTo((first.thingCount, first.hashHigh, first.hashLow)));
    }

    [Test]
    public void ComparatorMatchesTicksNotListIndices()
    {
        var first = Make((5, "Steel", 2, 3, 5, 100));
        var changed = Make((5, "Steel", 3, 3, 5, 100));
        var local = new List<StateFingerprintSample>
        {
            new() { tick = 600, mapFingerprints = [first] },
            new() { tick = 1200, mapFingerprints = [first] }
        };
        var remote = new List<StateFingerprintSample>
        {
            new() { tick = 1200, mapFingerprints = [changed] }
        };
        var message = StateFingerprintComparison.FirstMismatch(local, remote);
        Assert.That(message, Does.Contain("tick 1200"));
        Assert.That(message, Does.Contain("map 123"));
    }

    [Test]
    public void UnmatchedSamplesCannotTriggerDesync()
    {
        var first = Make((5, "Steel", 2, 3, 5, 100));
        var local = new List<StateFingerprintSample>
        {
            new() { tick = 600, mapFingerprints = [first] }
        };
        Assert.That(StateFingerprintComparison.FirstMismatch(local, []), Is.Null);
        Assert.That(StateFingerprintComparison.FirstMismatch(local,
            [new StateFingerprintSample { tick = 1200, mapFingerprints = [first] }]), Is.Null);
    }

    [Test]
    public void DetectMissingMapAtSameGlobalTick()
    {
        var first = Make((5, "Steel", 2, 3, 5, 100));
        var local = new List<StateFingerprintSample>
        {
            new() { tick = 600, mapFingerprints = [first] }
        };
        var remote = new List<StateFingerprintSample>
        {
            new() { tick = 600, mapFingerprints = [] }
        };
        Assert.That(StateFingerprintComparison.FirstMismatch(local, remote), Does.Contain("missing remotely"));
    }
}
