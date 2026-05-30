using System.Collections.ObjectModel;
using System.Globalization;
using TxtTyper.Helpers;
using TxtTyper.Models;
using TxtTyper.Services.Interfaces;

namespace TxtTyper.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ISnippetStorageService _snippetStorageService;
    private readonly ITypingWorkflowService _typingWorkflowService;
    private readonly IDialogService _dialogService;

    private CancellationTokenSource? _typingCancellationSource;
    private string _scriptContent = string.Empty;
    private string _lineNumbersText = "1";
    private string _snippetName = string.Empty;
    private Snippet? _selectedSnippet;
    private int _countdownSeconds = 5;
    private int _characterDelayMilliseconds = 35;
    private int _lineDelayMilliseconds = 250;
    private bool _preserveBlankLines = true;
    private bool _enableControlTokens = true;
    private bool _requireConfirmationBeforeStart;
    private string _statusMessage = "Ready.";
    private string _countdownDisplay = "--";
    private string _estimatedTimeDisplay = "--";
    private int _currentLineNumber;
    private bool _isTypingActive;

    public MainViewModel(
        ISnippetStorageService snippetStorageService,
        ITypingWorkflowService typingWorkflowService,
        IDialogService dialogService)
    {
        _snippetStorageService = snippetStorageService;
        _typingWorkflowService = typingWorkflowService;
        _dialogService = dialogService;

        StartTypingCommand = new AsyncRelayCommand(StartTypingAsync, CanStartTyping);
        StopCommand = new RelayCommand(EmergencyStop);
        ClearEditorCommand = new RelayCommand(ClearEditor, () => !IsTypingActive && !string.IsNullOrEmpty(ScriptContent));
        NewSnippetCommand = new RelayCommand(NewSnippet, () => !IsTypingActive);
        SaveSnippetCommand = new AsyncRelayCommand(SaveSnippetAsync, () => !IsTypingActive && !string.IsNullOrWhiteSpace(SnippetName));
        DeleteSnippetCommand = new AsyncRelayCommand(DeleteSnippetAsync, () => !IsTypingActive && SelectedSnippet is not null);
        LoadSnippetCommand = new RelayCommand(LoadSelectedSnippet, () => !IsTypingActive && SelectedSnippet is not null);
        UpdateEstimatedTimeDisplay();
    }

    public ObservableCollection<Snippet> Snippets { get; } = [];

    public string ScriptContent
    {
        get => _scriptContent;
        set
        {
            if (!SetProperty(ref _scriptContent, value))
            {
                return;
            }

            UpdateLineNumbers();
            UpdateEstimatedTimeDisplay();
            RaiseCommandStates();
        }
    }

    public string LineNumbersText
    {
        get => _lineNumbersText;
        private set => SetProperty(ref _lineNumbersText, value);
    }

    public string SnippetName
    {
        get => _snippetName;
        set
        {
            if (SetProperty(ref _snippetName, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Snippet? SelectedSnippet
    {
        get => _selectedSnippet;
        set
        {
            if (!SetProperty(ref _selectedSnippet, value))
            {
                return;
            }

            if (value is not null)
            {
                SnippetName = value.Name;
            }

            RaiseCommandStates();
        }
    }

    public int CountdownSeconds
    {
        get => _countdownSeconds;
        set
        {
            if (SetProperty(ref _countdownSeconds, Math.Max(0, value)))
            {
                UpdateEstimatedTimeDisplay();
            }
        }
    }

    public int CharacterDelayMilliseconds
    {
        get => _characterDelayMilliseconds;
        set
        {
            if (SetProperty(ref _characterDelayMilliseconds, Math.Max(0, value)))
            {
                UpdateEstimatedTimeDisplay();
            }
        }
    }

    public int LineDelayMilliseconds
    {
        get => _lineDelayMilliseconds;
        set
        {
            if (SetProperty(ref _lineDelayMilliseconds, Math.Max(0, value)))
            {
                UpdateEstimatedTimeDisplay();
            }
        }
    }

    public bool PreserveBlankLines
    {
        get => _preserveBlankLines;
        set
        {
            if (SetProperty(ref _preserveBlankLines, value))
            {
                UpdateEstimatedTimeDisplay();
            }
        }
    }

    public bool EnableControlTokens
    {
        get => _enableControlTokens;
        set
        {
            if (SetProperty(ref _enableControlTokens, value))
            {
                UpdateEstimatedTimeDisplay();
            }
        }
    }

    public bool RequireConfirmationBeforeStart
    {
        get => _requireConfirmationBeforeStart;
        set => SetProperty(ref _requireConfirmationBeforeStart, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string CountdownDisplay
    {
        get => _countdownDisplay;
        private set => SetProperty(ref _countdownDisplay, value);
    }

    public string EstimatedTimeDisplay
    {
        get => _estimatedTimeDisplay;
        private set => SetProperty(ref _estimatedTimeDisplay, value);
    }

    public int CurrentLineNumber
    {
        get => _currentLineNumber;
        private set
        {
            if (SetProperty(ref _currentLineNumber, value))
            {
                OnPropertyChanged(nameof(CurrentLineDisplay));
            }
        }
    }

    public string CurrentLineDisplay => CurrentLineNumber > 0
        ? CurrentLineNumber.ToString(CultureInfo.InvariantCulture)
        : "--";

    public bool IsTypingActive
    {
        get => _isTypingActive;
        private set
        {
            if (!SetProperty(ref _isTypingActive, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ActivityState));
            RaiseCommandStates();
        }
    }

    public string ActivityState => IsTypingActive ? "Active" : "Stopped";

    public AsyncRelayCommand StartTypingCommand { get; }

    public RelayCommand StopCommand { get; }

    public RelayCommand ClearEditorCommand { get; }

    public RelayCommand NewSnippetCommand { get; }

    public AsyncRelayCommand SaveSnippetCommand { get; }

    public AsyncRelayCommand DeleteSnippetCommand { get; }

    public RelayCommand LoadSnippetCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            var snippets = await _snippetStorageService.LoadAsync();
            ReplaceSnippets(snippets);
            StatusMessage = Snippets.Count == 0
                ? "Ready."
                : $"Ready. Loaded {Snippets.Count} saved snippet{(Snippets.Count == 1 ? string.Empty : "s")}.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Ready, but snippets could not be loaded.";
            _dialogService.ShowError($"txt-typer could not load snippets from {_snippetStorageService.StoragePath}:{Environment.NewLine}{ex.Message}", "txt-typer");
        }
    }

    public void EmergencyStop()
    {
        if (_typingCancellationSource is null)
        {
            return;
        }

        StatusMessage = "Emergency stop requested.";
        CountdownDisplay = "STOP";
        _typingCancellationSource.Cancel();
    }

    public void NotifyHotkeyRegistrationFailed()
    {
        StatusMessage = "Global emergency stop hotkey is unavailable. Use the STOP button instead.";
    }

    private bool CanStartTyping()
    {
        return !IsTypingActive && !string.IsNullOrWhiteSpace(ScriptContent);
    }

    private async Task StartTypingAsync()
    {
        if (string.IsNullOrWhiteSpace(ScriptContent))
        {
            _dialogService.ShowWarning("Enter or load a script before starting.", "txt-typer");
            return;
        }

        if (RequireConfirmationBeforeStart &&
            !_dialogService.Confirm(
                "Start the countdown now? Focus the target window before it reaches zero.",
                "Confirm Start"))
        {
            return;
        }

        IsTypingActive = true;
        CurrentLineNumber = 0;
        CountdownDisplay = "--";
        StatusMessage = "Preparing countdown.";

        _typingCancellationSource?.Dispose();
        _typingCancellationSource = new CancellationTokenSource();

        try
        {
            var progress = new Progress<TypingProgress>(UpdateProgress);
            await _typingWorkflowService.ExecuteAsync(
                ScriptContent,
                CreateTypingSettings(),
                progress,
                _typingCancellationSource.Token);

            StatusMessage = "Typing complete.";
            CountdownDisplay = "DONE";
            CurrentLineNumber = 0;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Typing stopped.";
            CountdownDisplay = "STOP";
            CurrentLineNumber = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = "Typing failed.";
            CountdownDisplay = "ERR";
            CurrentLineNumber = 0;
            _dialogService.ShowError($"txt-typer failed to simulate input:{Environment.NewLine}{ex.Message}", "txt-typer");
        }
        finally
        {
            IsTypingActive = false;
            _typingCancellationSource?.Dispose();
            _typingCancellationSource = null;
            UpdateEstimatedTimeDisplay();
        }
    }

    private void UpdateProgress(TypingProgress progress)
    {
        StatusMessage = progress.StatusMessage;
        CurrentLineNumber = progress.CurrentLineNumber;
        CountdownDisplay = progress.CountdownSecondsRemaining > 0
            ? progress.CountdownSecondsRemaining.ToString(CultureInfo.InvariantCulture)
            : "GO";
        EstimatedTimeDisplay = FormatDuration(progress.EstimatedTimeRemaining);
    }

    private void ClearEditor()
    {
        ScriptContent = string.Empty;
        StatusMessage = "Editor cleared.";
    }

    private void NewSnippet()
    {
        SelectedSnippet = null;
        SnippetName = string.Empty;
        StatusMessage = "Enter a snippet name and save when ready.";
    }

    private void LoadSelectedSnippet()
    {
        if (SelectedSnippet is null)
        {
            return;
        }

        ScriptContent = SelectedSnippet.Content;
        SnippetName = SelectedSnippet.Name;
        StatusMessage = $"Loaded snippet '{SelectedSnippet.Name}' into the editor.";
    }

    private async Task SaveSnippetAsync()
    {
        var normalizedName = SnippetName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            _dialogService.ShowWarning("Enter a snippet name before saving.", "txt-typer");
            return;
        }

        var duplicate = Snippets.FirstOrDefault(
            snippet => string.Equals(snippet.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

        if (duplicate is not null &&
            duplicate != SelectedSnippet &&
            !_dialogService.Confirm(
                $"A snippet named '{normalizedName}' already exists. Overwrite it?",
                "Overwrite Snippet"))
        {
            return;
        }

        if (SelectedSnippet is null)
        {
            SelectedSnippet = duplicate ?? new Snippet();
            if (!Snippets.Contains(SelectedSnippet))
            {
                Snippets.Add(SelectedSnippet);
            }
        }
        else if (duplicate is not null && duplicate != SelectedSnippet)
        {
            Snippets.Remove(duplicate);
        }

        SelectedSnippet.Name = normalizedName;
        SelectedSnippet.Content = ScriptContent;

        ReplaceSnippets(Snippets);
        SelectedSnippet = Snippets.FirstOrDefault(
            snippet => string.Equals(snippet.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

        await PersistSnippetsAsync();
        StatusMessage = $"Saved snippet '{normalizedName}'.";
    }

    private async Task DeleteSnippetAsync()
    {
        if (SelectedSnippet is null)
        {
            return;
        }

        if (!_dialogService.Confirm(
                $"Delete snippet '{SelectedSnippet.Name}'?",
                "Delete Snippet"))
        {
            return;
        }

        var deletedName = SelectedSnippet.Name;
        Snippets.Remove(SelectedSnippet);
        SelectedSnippet = null;
        SnippetName = string.Empty;

        await PersistSnippetsAsync();
        StatusMessage = $"Deleted snippet '{deletedName}'.";
    }

    private async Task PersistSnippetsAsync()
    {
        try
        {
            await _snippetStorageService.SaveAsync(Snippets);
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"txt-typer could not save snippets:{Environment.NewLine}{ex.Message}", "txt-typer");
        }
    }

    private void ReplaceSnippets(IEnumerable<Snippet> snippets)
    {
        var orderedSnippets = snippets
            .Where(snippet => !string.IsNullOrWhiteSpace(snippet.Name))
            .OrderBy(snippet => snippet.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Snippets.Clear();
        foreach (var snippet in orderedSnippets)
        {
            Snippets.Add(snippet);
        }
    }

    private void UpdateLineNumbers()
    {
        var lineCount = 1;
        for (var index = 0; index < ScriptContent.Length; index++)
        {
            if (ScriptContent[index] == '\n')
            {
                lineCount++;
            }
        }

        LineNumbersText = string.Join(Environment.NewLine, Enumerable.Range(1, lineCount));
    }

    private TypingSettings CreateTypingSettings()
    {
        return new TypingSettings
        {
            CountdownSeconds = CountdownSeconds,
            CharacterDelayMilliseconds = CharacterDelayMilliseconds,
            LineDelayMilliseconds = LineDelayMilliseconds,
            PreserveBlankLines = PreserveBlankLines,
            EnableControlTokens = EnableControlTokens
        };
    }

    private void UpdateEstimatedTimeDisplay()
    {
        if (IsTypingActive)
        {
            return;
        }

        EstimatedTimeDisplay = string.IsNullOrWhiteSpace(ScriptContent)
            ? "--"
            : FormatDuration(_typingWorkflowService.EstimateDuration(ScriptContent, CreateTypingSettings()));
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return "00:00";
        }

        var roundedDuration = TimeSpan.FromSeconds(Math.Ceiling(duration.TotalSeconds));
        return roundedDuration.TotalHours >= 1
            ? roundedDuration.ToString(@"h\:mm\:ss")
            : roundedDuration.ToString(@"mm\:ss");
    }

    private void RaiseCommandStates()
    {
        StartTypingCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        ClearEditorCommand.RaiseCanExecuteChanged();
        NewSnippetCommand.RaiseCanExecuteChanged();
        SaveSnippetCommand.RaiseCanExecuteChanged();
        DeleteSnippetCommand.RaiseCanExecuteChanged();
        LoadSnippetCommand.RaiseCanExecuteChanged();
    }
}
