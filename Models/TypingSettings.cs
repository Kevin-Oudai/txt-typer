namespace TxtTyper.Models;

public sealed class TypingSettings
{
    public int CountdownSeconds { get; init; }

    public int CharacterDelayMilliseconds { get; init; }

    public int LineDelayMilliseconds { get; init; }

    public bool PreserveBlankLines { get; init; }

    public bool EnableControlTokens { get; init; }
}
