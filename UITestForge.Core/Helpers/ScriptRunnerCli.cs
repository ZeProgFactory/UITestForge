using System.Text;

namespace UITestForge.Helpers;

/// <summary>
/// Non-interactive helpers shared by command-line front ends.
/// </summary>
public static class ScriptRunnerCli
{
   /// <summary>Resolves a script path (absolute, relative to cwd, or with a default extension).</summary>
   public static string? ResolveScriptPath(string name, string? baseFolder = null)
   {
      if (string.IsNullOrWhiteSpace(name)) return null;
      name = name.Trim().Trim('"');

      var candidates = new List<string>();
      void Add(string p)
      {
         candidates.Add(p);
         if (!Path.HasExtension(p)) { candidates.Add(p + ".df"); candidates.Add(p + ".devflow"); candidates.Add(p + ".txt"); }
      }

      if (Path.IsPathRooted(name)) Add(name);
      else
      {
         Add(Path.GetFullPath(name));
         if (!string.IsNullOrWhiteSpace(baseFolder)) Add(Path.Combine(baseFolder, name));
      }

      return candidates.FirstOrDefault(File.Exists);
   }

   /// <summary>Returns the first agent of the list, or null.</summary>
   public static DevFlowAgent? SelectFirstAgent(IEnumerable<DevFlowAgent>? agents)
      => agents?.FirstOrDefault();

   /// <summary>Counts failed steps (lines with a failure mark) in a log.</summary>
   public static int CountFailures(string log)
      => string.IsNullOrEmpty(log) ? 0 : log.Split('\n').Count(l => l.Contains('\u2717'));

   /// <summary>
   /// Fetches the agents, picks the first, runs the script and returns the exit code
   /// (0 = success, 1 = script failure, 2 = usage / no agent / script not found).
   /// </summary>
   public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken ct = default)
   {
      if (args.Length < 1 || args[0] is "-h" or "--help" or "/?")
      {
         error.WriteLine("Usage: UITF <scriptname>");
         return 2;
      }

      var scriptPath = ResolveScriptPath(args[0]);
      if (scriptPath is null)
      {
         error.WriteLine($"Script not found: {args[0]}");
         return 2;
      }

      await DevFlowCliHelper.EnsureBrokerStartedAsync();

      List<DevFlowAgent>? agents;
      try { agents = await DevFlowBrokerClient.FetchAgentsAsync(ct); }
      catch (Exception ex)
      {
         error.WriteLine($"Cannot reach DevFlow broker: {ex.Message}");
         return 2;
      }

      var agent = SelectFirstAgent(agents);
      if (agent is null)
      {
         error.WriteLine("No DevFlow agent connected.");
         return 2;
      }

      var forwardError = await DevFlowCliHelper.EnsureAgentPortForwardedAsync(agent);
      if (forwardError is not null)
      {
         error.WriteLine(forwardError);
         return 2;
      }

      output.WriteLine($"Agent : {agent.DisplayName} (port {agent.Port})");
      output.WriteLine($"Script: {scriptPath}");

      var script = await File.ReadAllTextAsync(scriptPath, ct);
      var printed = 0;
      var lastLog = string.Empty;

      var (_, runError) = await ScriptEditorHelper.RunScriptAsync(
         script,
         agent,
         onStepStatus: (_, _) => { },
         onOutputUpdate: text =>
         {
            lastLog = text;
            if (text.Length > printed)
            {
               output.Write(text.AsSpan(printed));
               printed = text.Length;
            }
         },
         onScreenshotCaptured: _ => { },
         onGetCurrentPage: null,
         scriptFolder: Path.GetDirectoryName(scriptPath),
         currentPageName: null);

      if (runError is not null)
      {
         error.WriteLine($"Script error: {runError.Message}");
         return 1;
      }

      return CountFailures(lastLog) > 0 ? 1 : 0;
   }
}
