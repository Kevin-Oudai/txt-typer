namespace TxtTyper.Models;

public sealed class TypingProgress
{
    public string StatusMessage { get; init; } = string.Empty;

    public int CountdownSecondsRemaining { get; init; }

    public int CurrentLineNumber { get; init; }

    public TimeSpan EstimatedTimeRemaining { get; init; }
}
