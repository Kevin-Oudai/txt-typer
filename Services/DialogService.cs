using System.Windows;
using TxtTyper.Services.Interfaces;

namespace TxtTyper.Services;

public sealed class DialogService : IDialogService
{
    public bool Confirm(string message, string title)
    {
        return MessageBox.Show(
                   message,
                   title,
                   MessageBoxButton.OKCancel,
                   MessageBoxImage.Question) == MessageBoxResult.OK;
    }

    public void ShowWarning(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
