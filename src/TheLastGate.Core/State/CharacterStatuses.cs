using System;

namespace TheLastGate.State;

public enum CharacterStatusKind
{
    Idle,
    StraightWalk,
    DiagonalWalk,
    Turn,
    Action,
    Unknown,
}

public readonly record struct CharacterStatusBlock(
    CharacterStatusKind Kind,
    int FirstStatus,
    int Length,
    int Direction);

public static class CharacterStatuses
{
    public static CharacterStatusBlock BlockOf(int status) => throw new NotImplementedException();
}
