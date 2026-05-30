using TxtTyper.Helpers;

namespace TxtTyper.Models;

public sealed class Snippet : ObservableObject
{
    private string _name = string.Empty;
    private string _content = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }
}
