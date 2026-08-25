using UITestForge;

namespace UITestForge.Helpers
{
#if !ANDROID
   /// <summary>
   /// Provides script execution capabilities for the UITestForge script editor.
   /// Supported commands: tap, fill, clear, focus, navigate, scroll, screenshot, wait, create-pptx, add-report-page, exit, goto, checkpage, checknpage, call.
   /// Labels can be defined with a colon (e.g., "label:").
   /// </summary>
   internal static class ScriptEditorHelper
   {
      /// <summary>
      /// Executes a multi-line script against the given agent.
      /// Tracks first and last screenshots for PowerPoint report generation via create-pptx command.
      /// </summary>
      /// <param name="scriptText">The raw script text from the editor.</param>
      /// <param name="agent">The DevFlow agent to run commands against.</param>
      /// <param name="onStepStatus">Invoked at the start of each step with <c>(stepNumber, commandName)</c>.</param>
      /// <param name="onOutputUpdate">Invoked after each step with the full accumulated log text.</param>
      /// <param name="onScreenshotCaptured">Invoked when a <c>screenshot</c> step succeeds, with the saved file path.</param>
      /// <param name="onGetCurrentPage">Optional callback to refresh and get the current page name. If null, uses <paramref name="currentPageName"/>.</param>
      /// <param name="scriptFolder">The folder to use for saving PPTX files when no absolute path is provided.</param>
      /// <param name="currentPageName">The current page name for checkpage command comparison.</param>
      /// <returns>The total number of steps executed and any unhandled exception.</returns>
      internal static async Task<(int StepCount, Exception? Error)> RunScriptAsync(
         string scriptText,
         DevFlowAgent agent,
         Action<int, string> onStepStatus,
         Action<string> onOutputUpdate,
         Action<string> onScreenshotCaptured,
         Func<Task<string?>>? onGetCurrentPage = null,
         string? scriptFolder = null,
         string? currentPageName = null)
      {
         var lines = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
         var log = new System.Text.StringBuilder();
         int stepNum = 0;

         // Track screenshots for PPTX generation
         string? firstScreenshot = null;
         string? lastScreenshot = null;

         // Build label dictionary for goto support
         var labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
         for (int i = 0; i < lines.Length; i++)
         {
            var trimmed = lines[i].Trim();
            if (trimmed.EndsWith(':') && !trimmed.Contains(' '))
            {
               var labelName = trimmed.TrimEnd(':');
               labels[labelName] = i;
            }
         }

         try
         {
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
               var rawLine = lines[lineIndex];
               var line = rawLine.Trim();
               if (line.Length == 0 || line.StartsWith('#')) continue;

               // Skip label definitions (they're just markers)
               if (line.EndsWith(':') && !line.Contains(' ')) continue;

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

                     // Track first and last screenshots
                     if (firstScreenshot == null)
                        firstScreenshot = tmpPath;
                     lastScreenshot = tmpPath;
                  }
                  else
                  {
                     resultLine = $"    \u2717 {stderr.Trim()}";
                  }
               }
               else if (cmd == "wait")
               {
                  if (!int.TryParse(rest.Trim(), out int seconds) || seconds < 0)
                  {
                     resultLine = "    ✗ wait requires a positive number of seconds";
                  }
                  else
                  {
                     await Task.Delay(seconds * 1000);
                     resultLine = $"    ✓ waited {seconds} second{(seconds != 1 ? "s" : "")}";
                  }
               }
               else if (cmd == "exit")
               {
                  resultLine = "    ✓ script execution stopped";
                  log.AppendLine(resultLine);
                  onOutputUpdate(log.ToString());
                  return (stepNum, null);
               }
               else if (cmd == "goto")
               {
                  if (string.IsNullOrWhiteSpace(rest))
                  {
                     resultLine = "    ✗ goto requires a label name";
                  }
                  else if (!labels.TryGetValue(rest.Trim(), out int targetLine))
                  {
                     resultLine = $"    ✗ label '{rest.Trim()}' not found";
                  }
                  else
                  {
                     resultLine = $"    ✓ jumping to {rest.Trim()}";
                     log.AppendLine(resultLine);
                     onOutputUpdate(log.ToString());
                     lineIndex = targetLine; // Jump to label (loop will increment)
                     continue;
                  }
               }
               else if (cmd == "checkpage")
               {
                  // Expected format: checkpage <pageName> <label>
                  var args = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                  if (args.Length < 2)
                  {
                     resultLine = "    ✗ checkpage requires a page name and label (e.g., checkpage MainPage myLabel)";
                  }
                  else
                  {
                     var expectedPage = args[0].Trim();
                     var targetLabel = args[1].Trim();

                     // Refresh TreeView to get current page name
                     var actualPageName = currentPageName;
                     if (onGetCurrentPage != null)
                     {
                        try
                        {
                           actualPageName = await onGetCurrentPage();
                        }
                        catch (Exception ex)
                        {
                           resultLine = $"    ✗ failed to refresh page name: {ex.Message}";
                           log.AppendLine(resultLine);
                           onOutputUpdate(log.ToString());
                           continue;
                        }
                     }

                     if (string.Equals(actualPageName, expectedPage, StringComparison.OrdinalIgnoreCase))
                     {
                        if (!labels.TryGetValue(targetLabel, out int targetLine))
                        {
                           resultLine = $"    ✗ label '{targetLabel}' not found";
                        }
                        else
                        {
                           resultLine = $"    ✓ page matches '{expectedPage}', jumping to {targetLabel}";
                           log.AppendLine(resultLine);
                           onOutputUpdate(log.ToString());
                           lineIndex = targetLine; // Jump to label
                           continue;
                        }
                     }
                     else
                     {
                        resultLine = $"    ○ page is '{actualPageName ?? "(null)"}', not '{expectedPage}' - skipping";
                     }
                  }
               }
               else if (cmd == "checknpage")
               {
                  // Expected format: checknpage <pageName> <label>
                  var args = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                  if (args.Length < 2)
                  {
                     resultLine = "    ✗ checknpage requires a page name and label (e.g., checknpage MainPage myLabel)";
                  }
                  else
                  {
                     var expectedPage = args[0].Trim();
                     var targetLabel = args[1].Trim();

                     // Refresh TreeView to get current page name
                     var actualPageName = currentPageName;
                     if (onGetCurrentPage != null)
                     {
                        try
                        {
                           actualPageName = await onGetCurrentPage();
                        }
                        catch (Exception ex)
                        {
                           resultLine = $"    ✗ failed to refresh page name: {ex.Message}";
                           log.AppendLine(resultLine);
                           onOutputUpdate(log.ToString());
                           continue;
                        }
                     }

                     if (!string.Equals(actualPageName, expectedPage, StringComparison.OrdinalIgnoreCase))
                     {
                        if (!labels.TryGetValue(targetLabel, out int targetLine))
                        {
                           resultLine = $"    ✗ label '{targetLabel}' not found";
                        }
                        else
                        {
                           resultLine = $"    ✓ page does not match '{expectedPage}', jumping to {targetLabel}";
                           log.AppendLine(resultLine);
                           onOutputUpdate(log.ToString());
                           lineIndex = targetLine; // Jump to label
                           continue;
                        }
                     }
                     else
                     {
                        resultLine = $"    ○ page is '{actualPageName}', matches '{expectedPage}' - skipping";
                     }
                  }
               }
               else if (cmd == "create-pptx")
               {
                  resultLine = await HandleCreatePptxAsync(rest, scriptText, log.ToString(), firstScreenshot, lastScreenshot, agent, scriptFolder);
               }
               else if (cmd == "add-report-page")
               {
                  resultLine = await HandleAddReportPageAsync(rest, scriptText, log.ToString(), firstScreenshot, lastScreenshot);
               }
               else if (cmd == "call")
               {
                  if (string.IsNullOrWhiteSpace(rest))
                  {
                     resultLine = "    ✗ call requires a script filename";
                  }
                  else
                  {
                     var scriptPath = rest.Trim('"');

                     // If not absolute path, try to resolve relative to scriptFolder or current directory
                     if (!Path.IsPathRooted(scriptPath))
                     {
                        if (!string.IsNullOrWhiteSpace(scriptFolder))
                        {
                           scriptPath = Path.Combine(scriptFolder, scriptPath);
                        }
                     }

                     if (!File.Exists(scriptPath))
                     {
                        resultLine = $"    ✗ script file not found: {scriptPath}";
                     }
                     else
                     {
                        try
                        {
                           var calledScript = await File.ReadAllTextAsync(scriptPath);
                           var calledScriptFolder = Path.GetDirectoryName(scriptPath);

                           log.AppendLine($"    → calling script: {scriptPath}");
                           onOutputUpdate(log.ToString());

                           var (calledSteps, calledError) = await RunScriptAsync(
                              calledScript,
                              agent,
                              onStepStatus,
                              onOutputUpdate,
                              onScreenshotCaptured,
                              onGetCurrentPage,
                              calledScriptFolder,
                              currentPageName);

                           stepNum += calledSteps;

                           if (calledError != null)
                           {
                              resultLine = $"    ✗ called script failed: {calledError.Message}";
                           }
                           else
                           {
                              resultLine = $"    ✓ called script completed ({calledSteps} steps)";
                           }
                        }
                        catch (Exception ex)
                        {
                           resultLine = $"    ✗ failed to execute called script: {ex.Message}";
                        }
                     }
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

            "screenshot" => string.Empty, // Handled specially in RunScriptAsync

            "wait" => string.Empty, // Handled specially in RunScriptAsync

            "exit" => string.Empty, // Handled specially in RunScriptAsync

            "goto" => string.Empty, // Handled specially in RunScriptAsync

            "checkpage" => string.Empty, // Handled specially in RunScriptAsync

            "checknpage" => string.Empty, // Handled specially in RunScriptAsync

            "create-pptx" => string.Empty, // Handled specially in RunScriptAsync

            "add-report-page" => string.Empty, // Handled specially in RunScriptAsync

            "call" => string.Empty, // Handled specially in RunScriptAsync

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

      /// <summary>
      /// Handles the <c>create-pptx</c> command to generate a PowerPoint report.
      /// Expected format: <c>create-pptx [filename] [title]</c>
      /// If filename is omitted, generates: report_yyyyMMdd_HHmmss.pptx
      /// If title is omitted, uses: Test Report
      /// </summary>
      private static async Task<string> HandleCreatePptxAsync(
         string rest,
         string scriptText,
         string executionLogs,
         string? beforeImagePath,
         string? afterImagePath,
         DevFlowAgent agent,
         string? scriptFolder)
      {
         try
         {
            // Parse arguments: [filename] [title]
            var parts = rest.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            var filename = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])
               ? parts[0].Trim('"')
               : $"report_{DateTime.Now:yyyyMMdd_HHmmss}.pptx";

            var title = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
               ? parts[1].Trim('"')
               : "Test Report";

            // Ensure .pptx extension
            if (!filename.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase))
               filename += ".pptx";

            // Generate full path
            // If filename is absolute, use it; otherwise, use scriptFolder if available, or fall back to AppDataDirectory
            string outputPath;
            if (Path.IsPathRooted(filename))
            {
               outputPath = filename;
            }
            else if (!string.IsNullOrWhiteSpace(scriptFolder))
            {
               outputPath = Path.Combine(scriptFolder, filename);
            }
            else
            {
               outputPath = Path.Combine(FileSystem.AppDataDirectory, filename);
            }

            // Extract base version (e.g., "0.1.0" from "0.1.0-preview.12.26368.2+...")
            var shortVersion = agent.Version?.Split(new[] { '-', '+' }, 2)[0] ?? agent.Version;

            // Create the PowerPoint report
            PptxReportHelper.CreateReport(
               outputPath,
               title,
               agent.AppName,
               agent.Tfm,
               shortVersion);

            return $"    ✓ PowerPoint created → {outputPath}";
         }
         catch (Exception ex)
         {
            return $"    ✗ Failed to create PowerPoint: {ex.Message}";
         }
      }

      /// <summary>
      /// Handles the <c>add-report-page</c> command to add a report page to the current PPTX.
      /// Expected format: <c>add-report-page [title]</c>
      /// If title is omitted, uses: Test Report
      /// Uses the first and last screenshots captured during script execution.
      /// </summary>
      private static Task<string> HandleAddReportPageAsync(
         string rest,
         string scriptText,
         string executionLogs,
         string? beforeImagePath,
         string? afterImagePath)
      {
         try
         {
            // Check if a PPTX file is currently open
            if (string.IsNullOrEmpty(PptxReportHelper.CurrentPPTXFile) || 
                !File.Exists(PptxReportHelper.CurrentPPTXFile))
            {
               return Task.FromResult("    \u2717 No PPTX file open. Use 'create-pptx' first.");
            }

            // Parse title argument
            var title = !string.IsNullOrWhiteSpace(rest)
               ? rest.Trim('"')
               : "Test Report";

            // Add the report page
            PptxReportHelper.AddReportPage(
               beforeImagePath,
               afterImagePath,
               executionLogs,
               scriptText,
               title);

            return Task.FromResult($"    \u2713 Report page added to {Path.GetFileName(PptxReportHelper.CurrentPPTXFile)}");
         }
         catch (Exception ex)
         {
            return Task.FromResult($"    \u2717 Failed to add report page: {ex.Message}");
         }
      }
   }
#endif
}
