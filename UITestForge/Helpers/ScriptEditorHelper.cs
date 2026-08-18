using UITestForge;

namespace UITestForge.Helpers
{
   internal static class ScriptEditorHelper
   {
      /// <summary>
      /// Executes a multi-line script against the given agent.
      /// </summary>
      /// <param name="scriptText">The raw script text from the editor.</param>
      /// <param name="agent">The DevFlow agent to run commands against.</param>
      /// <param name="onStepStatus">Invoked at the start of each step with <c>(stepNumber, commandName)</c>.</param>
      /// <param name="onOutputUpdate">Invoked after each step with the full accumulated log text.</param>
      /// <param name="onScreenshotCaptured">Invoked when a <c>screenshot</c> step succeeds, with the saved file path.</param>
      /// <returns>The total number of steps executed and any unhandled exception.</returns>
      internal static async Task<(int StepCount, Exception? Error)> RunScriptAsync(
         string scriptText,
         DevFlowAgent agent,
         Action<int, string> onStepStatus,
         Action<string> onOutputUpdate,
         Action<string> onScreenshotCaptured)
      {
         var lines = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
         var log = new System.Text.StringBuilder();
         int stepNum = 0;

         try
         {
            foreach (var rawLine in lines)
            {
               var line = rawLine.Trim();
               if (line.Length == 0 || line.StartsWith('#')) continue;

               stepNum++;
               var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
               var cmd = parts[0].ToLowerInvariant();
               var rest = parts.Length > 1 ? parts[1] : string.Empty;

               onStepStatus(stepNum, cmd);
               log.AppendLine($"[{stepNum}] {line}");

               string cliArgs;
               try
               {
                  cliArgs = BuildCliArgs(cmd, rest);
               }
               catch (ArgumentException ex)
               {
                  log.AppendLine($"    \u2717 {ex.Message}");
                  onOutputUpdate(log.ToString());
                  continue;
               }

               string resultLine;
               if (cmd == "screenshot")
               {
                  var tmpPath = string.IsNullOrWhiteSpace(rest)
                     ? Path.ChangeExtension(Path.GetTempFileName(), ".png")
                     : rest.Trim('"');
                  cliArgs = $"ui screenshot --output \"{tmpPath}\" --overwrite";
                  var (exitCode, _, stderr) = await DevFlowCliHelper.RunDevFlowAsync(cliArgs, agent);
                  if (exitCode == 0 && File.Exists(tmpPath))
                  {
                     onScreenshotCaptured(tmpPath);
                     resultLine = $"    \u2713 screenshot \u2192 {tmpPath}";
                  }
                  else
                  {
                     resultLine = $"    \u2717 {stderr.Trim()}";
                  }
               }
               else
               {
                  var (exitCode, stdout, stderr) = await DevFlowCliHelper.RunDevFlowAsync(cliArgs, agent);
                  var detail = stdout.Trim().Length > 0 ? stdout.Trim() : stderr.Trim();
                  resultLine = exitCode == 0
                     ? $"    \u2713 ok{(detail.Length > 0 ? " \u2014 " + detail : "")}"
                     : $"    \u2717 {(detail.Length > 0 ? detail : $"exit {exitCode}")}";
               }

               log.AppendLine(resultLine);
               onOutputUpdate(log.ToString());
            }

            return (stepNum, null);
         }
         catch (Exception ex)
         {
            log.AppendLine($"    \u2717 Error: {ex.Message}");
            onOutputUpdate(log.ToString());
            return (stepNum, ex);
         }
      }


      /// <summary>
      /// Translates a script command token and its arguments into a devflow CLI argument string.
      /// </summary>
      /// <exception cref="ArgumentException">Thrown when the command is unknown or arguments are missing.</exception>
      internal static string BuildCliArgs(string cmd, string rest)
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

            _ => throw new ArgumentException($"Unknown command: {cmd}")
         };

      /// <summary>
      /// Parses the arguments of a <c>fill</c> script command.
      /// Expected format: <c>fill &lt;automationId&gt; &lt;text...&gt;</c>
      /// </summary>
      private static string ParseFill(string rest)
      {
         // maui devflow ui fill --text --automationId PhoneNumberEntry abc

         var idx = rest.IndexOf(' ');
         if (idx < 0)
            throw new ArgumentException("fill requires an automationId and text (e.g. fill MyEntry Hello)");
         var id = rest[..idx].Trim();
         var text = rest[(idx + 1)..].Trim();

         return $"ui fill --text --automationId {id} \"{text}\"".Replace("\"\"", "\"");
      }

      /// <summary>
      /// Parses the arguments of a <c>scroll</c> script command.
      /// Supported formats:
      /// <list type="bullet">
      ///   <item><c>scroll &lt;automationId&gt;</c> — scrolls the element into view</item>
      ///   <item><c>scroll up [dy]</c> — scrolls up by <paramref name="dy"/> pixels (default 300)</item>
      ///   <item><c>scroll down [dy]</c> — scrolls down by <paramref name="dy"/> pixels (default 300)</item>
      /// </list>
      /// </summary>
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
            // Positive dy = up, negative dy = down (per CLI help)
            var dy = first == "up" ? delta : -delta;
            return $"ui scroll --dy {dy}";
         }

         // Treat as automationId — scroll element into view
         return $"ui scroll --element \"{parts[0].Trim()}\"";
      }
   }
}
