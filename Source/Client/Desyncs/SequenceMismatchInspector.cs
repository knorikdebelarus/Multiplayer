using System;
using System.Collections.Generic;

namespace Multiplayer.Client.Desyncs;

/// <summary>
/// Compares values already sampled by the multiplayer sync system.
/// This does not attribute a mismatch to a specific game tick or mod.
/// </summary>
internal static class SequenceMismatchInspector
{
    internal static SequenceMismatch<T>? FirstDifference<T>(
        IReadOnlyList<T> first, IReadOnlyList<T> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        int sharedCount = Math.Min(first.Count, second.Count);
        for (int i = 0; i < sharedCount; i++)
            if (!EqualityComparer<T>.Default.Equals(first[i], second[i]))
                return new SequenceMismatch<T>(
                    i, first.Count, second.Count, true, first[i], true, second[i]);

        if (first.Count == second.Count)
            return null;

        return new SequenceMismatch<T>(
            sharedCount, first.Count, second.Count,
            first.Count > sharedCount,
            first.Count > sharedCount ? first[sharedCount] : default!,
            second.Count > sharedCount,
            second.Count > sharedCount ? second[sharedCount] : default!);
    }
}

internal readonly record struct SequenceMismatch<T>(
    int Index,
    int FirstCount,
    int SecondCount,
    bool FirstPresent,
    T FirstValue,
    bool SecondPresent,
    T SecondValue)
{
    internal string Format(string firstName, string secondName)
    {
        string a = FirstPresent ? Convert.ToString(FirstValue) ?? "<null>" : "<missing>";
        string b = SecondPresent ? Convert.ToString(SecondValue) ?? "<null>" : "<missing>";
        return $"first difference at sample index {Index}; " +
               $"{firstName}={a}, {secondName}={b}; " +
               $"sample counts: {firstName}={FirstCount}, {secondName}={SecondCount}";
    }
}
