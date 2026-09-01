using SkiaSharp;
using ZPF.Maui.Script.ScriptEditing;

namespace UITestForge.Helpers;

/// <summary>
/// Syntax highlighter for UITestForge <c>.df</c> scripts.
/// Highlights: <c>#</c> comments, labels (<c>name:</c>), known commands,
/// quoted string arguments, numeric arguments and <c>goto</c> label targets.
/// Stateless - each line is highlighted independently.
/// </summary>
public sealed class UITestForgeScriptHighlighter : ISyntaxHighlighter
{
   /// <summary>All commands understood by <see cref="ScriptEditorHelper"/>.</summary>
   private static readonly HashSet<string> Commands = new(StringComparer.OrdinalIgnoreCase)
   {
      "tap", "fill", "clear", "focus", "navigate", "scroll", "screenshot", "wait",
      "create-pptx", "add-report-page", "addsummary", "exit", "goto",
      "checkpage", "checknpage", "isvisible", "isnvisible", "print", "call"
   };

   /// <summary>Commands whose first argument is a label / flow target.</summary>
   private static readonly HashSet<string> LabelTargetCommands = new(StringComparer.OrdinalIgnoreCase)
   {
      "goto", "call"
   };

   public void Reset() { }

   public IReadOnlyList<StyleSpan> GetSpans(string line, int lineIndex, ScriptPadTheme theme)
   {
      var spans = new List<StyleSpan>();
      if (line.Length == 0)
         return spans;

      string trimmed = line.TrimStart();
      int indent = line.Length - trimmed.Length;

      // full line comment
      if (trimmed.StartsWith('#'))
      {
         spans.Add(new StyleSpan(0, line.Length, theme.Comment, theme.Transparent, TextStyle.Italic));
         return spans;
      }

      // label definition: "name:" with no spaces
      string trimmedBoth = trimmed.TrimEnd();
      if (trimmedBoth.Length > 1 && trimmedBoth.EndsWith(':') && !trimmedBoth.Contains(' '))
      {
         spans.Add(new StyleSpan(indent, trimmedBoth.Length, theme.Heading, theme.Transparent, TextStyle.Bold));
         return spans;
      }

      // default foreground for the whole line
      spans.Add(new StyleSpan(0, line.Length, theme.Foreground, theme.Transparent, TextStyle.Normal));

      // command token
      int cmdStart = indent;
      int cmdEnd = cmdStart;
      while (cmdEnd < line.Length && !char.IsWhiteSpace(line[cmdEnd])) cmdEnd++;

      string command = line[cmdStart..cmdEnd];
      bool known = Commands.Contains(command);

      if (command.Length > 0)
      {
         spans.Add(new StyleSpan(
            cmdStart,
            command.Length,
            known ? theme.Keyword : theme.Emphasis,
            theme.Transparent,
            known ? TextStyle.Bold : TextStyle.Underline));
      }

      // first argument of goto/call is a label target
      if (known && LabelTargetCommands.Contains(command))
      {
         int argStart = cmdEnd;
         while (argStart < line.Length && char.IsWhiteSpace(line[argStart])) argStart++;

         int argEnd = argStart;
         while (argEnd < line.Length && !char.IsWhiteSpace(line[argEnd])) argEnd++;

         if (argEnd > argStart && line[argStart] != '"')
            spans.Add(new StyleSpan(argStart, argEnd - argStart, theme.Link, theme.Transparent, TextStyle.Underline));
      }

      ScanArguments(line, cmdEnd, spans, theme);

      spans.Sort((a, b) => a.Start.CompareTo(b.Start));
      return spans;
   }

   /// <summary>Highlights quoted strings, numbers and trailing comments after the command token.</summary>
   private static void ScanArguments(string line, int start, List<StyleSpan> spans, ScriptPadTheme theme)
   {
      int i = start;
      while (i < line.Length)
      {
         char c = line[i];

         if (c == '"')
         {
            int close = line.IndexOf('"', i + 1);
            int length = close < 0 ? line.Length - i : close - i + 1;
            spans.Add(new StyleSpan(i, length, theme.StringLiteral, theme.Transparent, TextStyle.Normal));
            i += length;
            continue;
         }

         if (c == '#')
         {
            spans.Add(new StyleSpan(i, line.Length - i, theme.Comment, theme.Transparent, TextStyle.Italic));
            return;
         }

         if (char.IsDigit(c) && (i == 0 || !IsWordChar(line[i - 1])))
         {
            int numEnd = i;
            while (numEnd < line.Length && (char.IsDigit(line[numEnd]) || line[numEnd] is '.' or ',')) numEnd++;

            if (numEnd >= line.Length || !IsWordChar(line[numEnd]))
            {
               spans.Add(new StyleSpan(i, numEnd - i, theme.Code, theme.Transparent, TextStyle.Normal));
               i = numEnd;
               continue;
            }

            while (numEnd < line.Length && IsWordChar(line[numEnd])) numEnd++;
            i = numEnd;
            continue;
         }

         i++;
      }
   }

   private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '-';
}
