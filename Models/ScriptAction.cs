namespace TxtTyper.Models;

public sealed class ScriptAction
{
    public ScriptActionKind Kind { get; init; }

    public string Text { get; init; } = string.Empty;

    public int DelayMilliseconds { get; init; }

    public IReadOnlyList<ushort> VirtualKeys { get; init; } = Array.Empty<ushort>();

    public static ScriptAction FromText(string text)
    {
        return new ScriptAction
        {
            Kind = ScriptActionKind.Text,
            Text = text
        };
    }

    public static ScriptAction Wait(int delayMilliseconds)
    {
        return new ScriptAction
        {
            Kind = ScriptActionKind.Wait,
            DelayMilliseconds = delayMilliseconds
        };
    }

    public static ScriptAction KeyChord(params ushort[] virtualKeys)
    {
        return new ScriptAction
        {
            Kind = ScriptActionKind.KeyChord,
            VirtualKeys = virtualKeys
        };
    }
}
