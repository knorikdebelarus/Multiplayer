using Multiplayer.Client.Desyncs;

namespace Tests;

public class SequenceMismatchInspectorTest
{
    [Test]
    public void EqualSequencesHaveNoMismatch()
    {
        Assert.That(SequenceMismatchInspector.FirstDifference(new uint[] { 5, 6 }, new uint[] { 5, 6 }), Is.Null);
        Assert.That(SequenceMismatchInspector.FirstDifference(Array.Empty<uint>(), Array.Empty<uint>()), Is.Null);
    }

    [Test]
    public void FindsTheFirstDifferentValueWithCorrectCounts()
    {
        var result = SequenceMismatchInspector.FirstDifference(
            new uint[] { 8, 12, 31 },
            new uint[] { 8, 99, 20 });

        Assert.That(result, Is.Not.Null);
        var mismatch = result!.Value;
        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Index, Is.EqualTo(1));
            Assert.That(mismatch.FirstCount, Is.EqualTo(3));
            Assert.That(mismatch.SecondCount, Is.EqualTo(3));
            Assert.That(mismatch.FirstValue, Is.EqualTo(12u));
            Assert.That(mismatch.SecondValue, Is.EqualTo(99u));
        });
        Assert.That(mismatch.Format("local", "remote"), Does.Contain("sample index 1"));
    }

    [Test]
    public void ReportsMissingRemoteValueInsteadOfDefaultZero()
    {
        var mismatch = SequenceMismatchInspector.FirstDifference(
            new uint[] { 10, 0 },
            new uint[] { 10 })!.Value;

        Assert.That(mismatch.Index, Is.EqualTo(1));
        Assert.That(mismatch.FirstPresent, Is.True);
        Assert.That(mismatch.SecondPresent, Is.False);
        Assert.That(mismatch.Format("local", "remote"), Does.Contain("remote=<missing>"));
    }

    [Test]
    public void ReportsMissingLocalValue()
    {
        var mismatch = SequenceMismatchInspector.FirstDifference(
            new int[] { 10 },
            new int[] { 10, 123 })!.Value;

        Assert.That(mismatch.Index, Is.EqualTo(1));
        Assert.That(mismatch.FirstPresent, Is.False);
        Assert.That(mismatch.SecondPresent, Is.True);
        Assert.That(mismatch.Format("local", "remote"), Does.Contain("local=<missing>"));
    }

    [Test]
    public void SameLengthDifferentTraceHashIsDetected()
    {
        var mismatch = SequenceMismatchInspector.FirstDifference(
            new int[] { 1, 2, 3, 4 },
            new int[] { 1, 2, 3, 5 })!.Value;

        Assert.That(mismatch.Index, Is.EqualTo(3));
    }
}
