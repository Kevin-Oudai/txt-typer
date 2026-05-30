using System.Windows;
using System.Windows.Input;
using TxtTyper.Models;

namespace TxtTyper.Services.Interfaces;

public interface IInputSimulationService
{
    Task TypeTextAsync(string text, int characterDelayMilliseconds, CancellationToken cancellationToken);

    Task SendKeyChordAsync(IReadOnlyList<ushort> virtualKeys, CancellationToken cancellationToken);

    Task DelayAsync(int delayMilliseconds, CancellationToken cancellationToken);
}

public interface IScriptTokenParser
{
    ParsedScriptLine ParseLine(string line);
}

public interface ISnippetStorageService
{
    string StoragePath { get; }

    Task<IReadOnlyList<Snippet>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IEnumerable<Snippet> snippets, CancellationToken cancellationToken = default);
}

public interface ITypingWorkflowService
{
    TimeSpan EstimateDuration(string script, TypingSettings settings);

    Task ExecuteAsync(
        string script,
        TypingSettings settings,
        IProgress<TypingProgress> progress,
        CancellationToken cancellationToken);
}

public interface IDialogService
{
    bool Confirm(string message, string title);

    void ShowWarning(string message, string title);

    void ShowError(string message, string title);
}

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;

    bool Register(Window window, ModifierKeys modifiers, Key key);

    void Unregister(Window window);
}
