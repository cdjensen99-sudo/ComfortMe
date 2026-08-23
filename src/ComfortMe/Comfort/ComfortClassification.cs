namespace ComfortMe;

internal enum ComfortVerdict
{
    Skip,
    NewGroup,
    Upgrade,
    Redundant,
    Unlit,
    Downgrade,
}

internal readonly struct ComfortClassification
{
    internal ComfortClassification(
        ComfortVerdict verdict,
        Piece.ComfortGroup group,
        int current,
        int candidate)
    {
        Verdict = verdict;
        Group = group;
        Current = current;
        Candidate = candidate;
        Delta = candidate - current;
    }

    internal ComfortVerdict Verdict { get; }

    internal Piece.ComfortGroup Group { get; }

    internal int Current { get; }

    internal int Candidate { get; }

    internal int Delta { get; }

    internal bool IsUpgrade => Verdict == ComfortVerdict.Upgrade || Verdict == ComfortVerdict.NewGroup;
}
