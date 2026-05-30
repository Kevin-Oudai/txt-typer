using System.Windows;
using TxtTyper.Services;
using TxtTyper.Services.Interfaces;
using TxtTyper.ViewModels;
using TxtTyper.Views;

namespace TxtTyper;

public partial class App : Application
{
    private IGlobalHotkeyService? _hotkeyService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var snippetStorageService = new SnippetStorageService();
        var dialogService = new DialogService();
        var inputSimulationService = new InputSimulationService();
        var scriptTokenParser = new ScriptTokenParser();
        var typingWorkflowService = new TypingWorkflowService(inputSimulationService, scriptTokenParser);
        _hotkeyService = new GlobalHotkeyService();

        var mainViewModel = new MainViewModel(
            snippetStorageService,
            typingWorkflowService,
            dialogService);

        var window = new MainWindow(mainViewModel, _hotkeyService);
        MainWindow = window;
        window.Show();

        _ = InitializeViewModelAsync(mainViewModel);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        base.OnExit(e);
    }

    private static async Task InitializeViewModelAsync(MainViewModel mainViewModel)
    {
        await mainViewModel.InitializeAsync();
    }
}
