namespace UITestForge.Helpers;

public static class ScriptCommandTranslator
{
    public static string BuildCliArgs(string cmd, string rest)
        => cmd switch
        {
            "tap" => string.IsNullOrWhiteSpace(rest)
                ? throw new ArgumentException("tap requires an automationId")
                : $"ui tap --automationId \"{rest.Trim()}\"",

            "fill" => ParseFill(rest),

            "clear" => string.IsNullOrWhiteSpace(rest)
                ? throw new ArgumentException("clear requires an automationId")
                : $"ui clear --automationId \"{rest.Trim()}\"",

            "focus" => string.IsNullOrWhiteSpace(rest)
                ? throw new ArgumentException("focus requires an automationId")
                : $"ui focus --automationId \"{rest.Trim()}\"",

            "navigate" => string.IsNullOrWhiteSpace(rest)
                ? throw new ArgumentException("navigate requires a Shell route (e.g. navigate //home)")
                : $"ui navigate {rest.Trim()}",

            "scroll" => ParseScroll(rest),

            "screenshot" or "wait" or "swipeback" or "exit" or "goto"
                or "checkpage" or "checknpage" or "isvisible" or "isnvisible"
                or "print" or "create-pptx" or "add-report-page" or "addsummary" or "call" => string.Empty,

            _ => throw new ArgumentException($"Unknown command: {cmd}")
        };

    private static string ParseFill(string rest)
    {
        var idx = rest.IndexOf(' ');
        if (idx < 0)
            throw new ArgumentException("fill requires an automationId and text (e.g. fill MyEntry Hello)");
        var id = rest[..idx].Trim();
        var text = rest[(idx + 1)..].Trim();

        return $"ui fill --text --automationId {id} \"{text}\"".Replace("\"\"", "\"");
    }

    private static string ParseScroll(string rest)
    {
        if (string.IsNullOrWhiteSpace(rest))
            throw new ArgumentException(
                "scroll requires a direction or automationId (e.g. scroll down, scroll up 500, scroll MyList)");

        var parts = rest.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var first = parts[0].ToLowerInvariant();

        if (first is "up" or "down")
        {
            int delta = 300;
            if (parts.Length > 1 && int.TryParse(parts[1], out var parsed))
                delta = parsed;
            var dy = first == "up" ? delta : -delta;
            return $"ui scroll --dy {dy}";
        }

        return $"ui scroll --element \"{parts[0].Trim()}\"";
    }
}

