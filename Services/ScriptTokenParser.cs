using System.Text;
using TxtTyper.Helpers;
using TxtTyper.Models;
using TxtTyper.Services.Interfaces;

namespace TxtTyper.Services;

public sealed class ScriptTokenParser : IScriptTokenParser
{
    private static readonly IReadOnlyDictionary<string, ushort[]> TokenMap =
        new Dictionary<string, ushort[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["ENTER"] = [VirtualKeys.Enter],
            ["TAB"] = [VirtualKeys.Tab],
            ["ESC"] = [VirtualKeys.Escape],
            ["UP"] = [VirtualKeys.Up],
            ["DOWN"] = [VirtualKeys.Down],
            ["LEFT"] = [VirtualKeys.Left],
            ["RIGHT"] = [VirtualKeys.Right],
            ["BACKSPACE"] = [VirtualKeys.Backspace],
            ["DELETE"] = [VirtualKeys.Delete],
            ["HOME"] = [VirtualKeys.Home],
            ["END"] = [VirtualKeys.End],
            ["PGUP"] = [VirtualKeys.PageUp],
            ["PGDN"] = [VirtualKeys.PageDown],
            ["CTRL+C"] = [VirtualKeys.Control, VirtualKeys.C],
            ["CTRL+V"] = [VirtualKeys.Control, VirtualKeys.V],
            ["CTRL+SHIFT+V"] = [VirtualKeys.Control, VirtualKeys.Shift, VirtualKeys.V],
            ["ALT+TAB"] = [VirtualKeys.Alt, VirtualKeys.Tab]
        };

    public ParsedScriptLine ParseLine(string line)
    {
        if (TryParseWaitOnlyLine(line, out var waitMilliseconds))
        {
            return new ParsedScriptLine([ScriptAction.Wait(waitMilliseconds)], suppressAutomaticEnter: true);
        }

        var actions = new List<ScriptAction>();
        var pendingText = new StringBuilder();
        var index = 0;

        while (index < line.Length)
        {
            if (line[index] != '{')
            {
                pendingText.Append(line[index]);
                index++;
                continue;
            }

            var closingBraceIndex = line.IndexOf('}', index + 1);
            if (closingBraceIndex < 0)
            {
                pendingText.Append(line[index]);
                index++;
                continue;
            }

            var tokenBody = line[(index + 1)..closingBraceIndex];
            if (!TryParseToken(tokenBody, out var action))
            {
                pendingText.Append(line[index]);
                index++;
                continue;
            }

            FlushPendingText(actions, pendingText);
            actions.Add(action);
            index = closingBraceIndex + 1;
        }

        FlushPendingText(actions, pendingText);
        return new ParsedScriptLine(actions, suppressAutomaticEnter: false);
    }

    private static void FlushPendingText(ICollection<ScriptAction> actions, StringBuilder pendingText)
    {
        if (pendingText.Length == 0)
        {
            return;
        }

        actions.Add(ScriptAction.FromText(pendingText.ToString()));
        pendingText.Clear();
    }

    private static bool TryParseToken(string tokenBody, out ScriptAction action)
    {
        action = null!;

        if (TryParseWaitToken(tokenBody, out var waitMilliseconds))
        {
            action = ScriptAction.Wait(waitMilliseconds);
            return true;
        }

        if (!TokenMap.TryGetValue(tokenBody.Trim(), out var virtualKeys))
        {
            return false;
        }

        action = ScriptAction.KeyChord(virtualKeys);
        return true;
    }

    private static bool TryParseWaitOnlyLine(string line, out int waitMilliseconds)
    {
        waitMilliseconds = 0;

        var trimmed = line.Trim();
        if (trimmed.Length < 8 || !trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
        {
            return false;
        }

        return TryParseWaitToken(trimmed[1..^1], out waitMilliseconds);
    }

    private static bool TryParseWaitToken(string tokenBody, out int waitMilliseconds)
    {
        waitMilliseconds = 0;

        var normalized = tokenBody.Trim();
        if (!normalized.StartsWith("WAIT:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rawValue = normalized[5..];
        return int.TryParse(rawValue, out waitMilliseconds) && waitMilliseconds >= 0;
    }
}
