using TxtTyper.Helpers;
using TxtTyper.Models;
using TxtTyper.Services.Interfaces;

namespace TxtTyper.Services;

public sealed class TypingWorkflowService : ITypingWorkflowService
{
    private readonly IInputSimulationService _inputSimulationService;
    private readonly IScriptTokenParser _scriptTokenParser;

    public TypingWorkflowService(
        IInputSimulationService inputSimulationService,
        IScriptTokenParser scriptTokenParser)
    {
        _inputSimulationService = inputSimulationService;
        _scriptTokenParser = scriptTokenParser;
    }

    public Task ExecuteAsync(
        string script,
        TypingSettings settings,
        IProgress<TypingProgress> progress,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            async () =>
            {
                var lines = SplitLines(script);
                var lineDurations = lines
                    .Select(line => EstimateLineDurationMilliseconds(line, settings))
                    .ToArray();
                var remainingMilliseconds = GetCountdownDurationMilliseconds(settings) + lineDurations.Sum();

                remainingMilliseconds = await RunCountdownAsync(
                    settings.CountdownSeconds,
                    progress,
                    cancellationToken,
                    remainingMilliseconds);

                for (var index = 0; index < lines.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var lineNumber = index + 1;
                    progress.Report(new TypingProgress
                    {
                        StatusMessage = $"Typing line {lineNumber} of {lines.Count}.",
                        CurrentLineNumber = lineNumber,
                        EstimatedTimeRemaining = ToTimeSpan(remainingMilliseconds)
                    });

                    var line = lines[index];
                    if (line.Length == 0)
                    {
                        if (!settings.PreserveBlankLines)
                        {
                            continue;
                        }

                        await _inputSimulationService.SendKeyChordAsync([VirtualKeys.Enter], cancellationToken);
                        await _inputSimulationService.DelayAsync(settings.LineDelayMilliseconds, cancellationToken);
                        remainingMilliseconds = Math.Max(0, remainingMilliseconds - lineDurations[index]);
                        continue;
                    }

                    if (settings.EnableControlTokens)
                    {
                        var parsedLine = _scriptTokenParser.ParseLine(line);
                        foreach (var action in parsedLine.Actions)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            switch (action.Kind)
                            {
                                case ScriptActionKind.Text:
                                    await _inputSimulationService.TypeTextAsync(
                                        action.Text,
                                        settings.CharacterDelayMilliseconds,
                                        cancellationToken);
                                    break;
                                case ScriptActionKind.Wait:
                                    await _inputSimulationService.DelayAsync(action.DelayMilliseconds, cancellationToken);
                                    break;
                                case ScriptActionKind.KeyChord:
                                    await _inputSimulationService.SendKeyChordAsync(action.VirtualKeys, cancellationToken);
                                    break;
                            }
                        }

                        if (!parsedLine.SuppressAutomaticEnter)
                        {
                            await _inputSimulationService.SendKeyChordAsync([VirtualKeys.Enter], cancellationToken);
                            await _inputSimulationService.DelayAsync(settings.LineDelayMilliseconds, cancellationToken);
                        }

                        remainingMilliseconds = Math.Max(0, remainingMilliseconds - lineDurations[index]);
                        continue;
                    }

                    await _inputSimulationService.TypeTextAsync(
                        line,
                        settings.CharacterDelayMilliseconds,
                        cancellationToken);

                    await _inputSimulationService.SendKeyChordAsync([VirtualKeys.Enter], cancellationToken);
                    await _inputSimulationService.DelayAsync(settings.LineDelayMilliseconds, cancellationToken);

                    remainingMilliseconds = Math.Max(0, remainingMilliseconds - lineDurations[index]);
                }

                progress.Report(new TypingProgress
                {
                    StatusMessage = "Typing complete.",
                    EstimatedTimeRemaining = TimeSpan.Zero
                });
            },
            cancellationToken);
    }

    public TimeSpan EstimateDuration(string script, TypingSettings settings)
    {
        var totalMilliseconds = GetCountdownDurationMilliseconds(settings);
        foreach (var line in SplitLines(script))
        {
            totalMilliseconds += EstimateLineDurationMilliseconds(line, settings);
        }

        return ToTimeSpan(totalMilliseconds);
    }

    private static IReadOnlyList<string> SplitLines(string script)
    {
        var normalized = script.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return normalized.Split('\n');
    }

    private async Task<long> RunCountdownAsync(
        int countdownSeconds,
        IProgress<TypingProgress> progress,
        CancellationToken cancellationToken,
        long remainingMilliseconds)
    {
        for (var remaining = countdownSeconds; remaining > 0; remaining--)
        {
            progress.Report(new TypingProgress
            {
                StatusMessage = $"Countdown: {remaining} second{(remaining == 1 ? string.Empty : "s")} remaining.",
                CountdownSecondsRemaining = remaining,
                EstimatedTimeRemaining = ToTimeSpan(remainingMilliseconds)
            });

            await _inputSimulationService.DelayAsync(1000, cancellationToken);
            remainingMilliseconds = Math.Max(0, remainingMilliseconds - 1000);
        }

        progress.Report(new TypingProgress
        {
            StatusMessage = "Typing into the currently focused window.",
            EstimatedTimeRemaining = ToTimeSpan(remainingMilliseconds)
        });

        return remainingMilliseconds;
    }

    private long EstimateLineDurationMilliseconds(string line, TypingSettings settings)
    {
        var characterDelay = Math.Max(0, settings.CharacterDelayMilliseconds);
        var lineDelay = Math.Max(0, settings.LineDelayMilliseconds);

        if (line.Length == 0)
        {
            return settings.PreserveBlankLines ? lineDelay : 0;
        }

        if (!settings.EnableControlTokens)
        {
            return ((long)line.Length * characterDelay) + lineDelay;
        }

        var parsedLine = _scriptTokenParser.ParseLine(line);
        long totalMilliseconds = 0;

        foreach (var action in parsedLine.Actions)
        {
            totalMilliseconds += action.Kind switch
            {
                ScriptActionKind.Text => (long)action.Text.Length * characterDelay,
                ScriptActionKind.Wait => Math.Max(0, action.DelayMilliseconds),
                _ => 0
            };
        }

        if (!parsedLine.SuppressAutomaticEnter)
        {
            totalMilliseconds += lineDelay;
        }

        return totalMilliseconds;
    }

    private static long GetCountdownDurationMilliseconds(TypingSettings settings)
    {
        return Math.Max(0, settings.CountdownSeconds) * 1000L;
    }

    private static TimeSpan ToTimeSpan(long totalMilliseconds)
    {
        return TimeSpan.FromMilliseconds(Math.Max(0, totalMilliseconds));
    }
}
