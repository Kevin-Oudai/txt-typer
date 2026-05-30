using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TxtTyper.Helpers;
using TxtTyper.Services.Interfaces;
using TxtTyper.ViewModels;

namespace TxtTyper.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IGlobalHotkeyService _hotkeyService;
    private ScrollViewer? _editorScrollViewer;
    private ScrollViewer? _lineNumbersScrollViewer;

    public MainWindow(MainViewModel viewModel, IGlobalHotkeyService hotkeyService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _hotkeyService = hotkeyService;
        DataContext = viewModel;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        var registered = _hotkeyService.Register(this, ModifierKeys.Control | ModifierKeys.Alt, Key.F12);
        if (!registered)
        {
            _viewModel.NotifyHotkeyRegistrationFailed();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _editorScrollViewer = VisualTreeHelpers.FindDescendant<ScrollViewer>(EditorTextBox);
        _lineNumbersScrollViewer = VisualTreeHelpers.FindDescendant<ScrollViewer>(LineNumbersTextBox);

        if (_editorScrollViewer is not null)
        {
            _editorScrollViewer.ScrollChanged += OnEditorScrollChanged;
        }

        _lineNumbersScrollViewer?.ScrollToVerticalOffset(0);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_editorScrollViewer is not null)
        {
            _editorScrollViewer.ScrollChanged -= OnEditorScrollChanged;
        }

        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _hotkeyService.Unregister(this);
    }

    private void OnEditorScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _lineNumbersScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        _viewModel.EmergencyStop();
    }
}
