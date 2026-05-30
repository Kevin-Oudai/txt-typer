namespace TxtTyper.Models;

public sealed class ParsedScriptLine
{
    public ParsedScriptLine(IReadOnlyList<ScriptAction> actions, bool suppressAutomaticEnter)
    {
        Actions = actions;
        SuppressAutomaticEnter = suppressAutomaticEnter;
    }

    public IReadOnlyList<ScriptAction> Actions { get; }

    public bool SuppressAutomaticEnter { get; }
}
