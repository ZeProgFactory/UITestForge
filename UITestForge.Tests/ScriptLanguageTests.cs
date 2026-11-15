using UITestForge.Helpers;
using ZPF.Maui.Script.ScriptEditing;

namespace UITestForge.Tests;

[TestClass]
public sealed class ScriptLanguageTests
{
    [TestMethod]
    [DataRow("tap", "LoginBtn", "ui tap --automationId \"LoginBtn\"")]
    [DataRow("clear", "EmailEntry", "ui clear --automationId \"EmailEntry\"")]
    [DataRow("navigate", "//home", "ui navigate //home")]
    [DataRow("fill", "EmailEntry user@example.com", "ui fill --text --automationId EmailEntry \"user@example.com\"")]
    [DataRow("scroll", "down", "ui scroll --dy -300")]
    [DataRow("scroll", "up 500", "ui scroll --dy 500")]
    [DataRow("scroll", "SaveButton", "ui scroll --element \"SaveButton\"")]
    public void BuildCliArgs_TranslatesSupportedCommands(string command, string arguments, string expected)
    {
        var actual = ScriptCommandTranslator.BuildCliArgs(command, arguments);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void BuildCliArgs_RejectsMissingRequiredArguments()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ScriptCommandTranslator.BuildCliArgs("tap", " "));
        Assert.ThrowsExactly<ArgumentException>(() => ScriptCommandTranslator.BuildCliArgs("fill", "EmailEntry"));
        Assert.ThrowsExactly<ArgumentException>(() => ScriptCommandTranslator.BuildCliArgs("scroll", ""));
    }

    [TestMethod]
    public void BuildCliArgs_RejectsUnknownCommands()
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(
            () => ScriptCommandTranslator.BuildCliArgs("tapp", "LoginBtn"));

        Assert.AreEqual("Unknown command: tapp", exception.Message);
    }

    [TestMethod]
    public void Highlighter_LeavesBlankLinesUnstyled()
    {
        var highlighter = new UITestForgeScriptHighlighter();

        var spans = highlighter.GetSpans("", 0, ScriptPadTheme.Light());

        Assert.AreEqual(0, spans.Count);
    }

    [TestMethod]
    public void Highlighter_StylesCommentsAndIndentedLabels()
    {
        var highlighter = new UITestForgeScriptHighlighter();
        var theme = ScriptPadTheme.Light();
        const string comment = "  # explanation";
        const string label = "  login:";

        var commentSpan = highlighter.GetSpans(comment, 0, theme).Single();
        var labelSpan = highlighter.GetSpans(label, 1, theme).Single();

        Assert.AreEqual(0, commentSpan.Start);
        Assert.AreEqual(comment.Length, commentSpan.Length);
        Assert.AreEqual(2, labelSpan.Start);
        Assert.AreEqual("login:".Length, labelSpan.Length);
    }

    [TestMethod]
    public void Highlighter_StylesCommandStringAndTrailingCommentRanges()
    {
        var highlighter = new UITestForgeScriptHighlighter();
        const string line = "tap Button \"Main Page\" # note";

        var spans = highlighter.GetSpans(line, 0, ScriptPadTheme.Light());
        int stringStart = line.IndexOf('"');
        int commentStart = line.IndexOf('#');

        Assert.IsTrue(spans.Any(span => span.Start == 0 && span.Length == 3));
        Assert.IsTrue(spans.Any(span => span.Start == stringStart && span.Length == "\"Main Page\"".Length));
        Assert.IsTrue(spans.Any(span => span.Start == commentStart && span.Length == line.Length - commentStart));
    }
}
